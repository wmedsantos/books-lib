#!/usr/bin/env python3
"""Import a Libib catalog CSV into the Books Library PostgreSQL database."""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from dataclasses import asdict, dataclass, field
from datetime import date, datetime, timezone
from pathlib import Path
from typing import Any

try:
    import psycopg
    from psycopg.rows import dict_row
except ImportError:  # pragma: no cover - exercised by the operator.
    psycopg = None
    dict_row = None


DEFAULT_DATABASE_URL = "postgresql://bookslib:bookslib_dev@localhost:5432/bookslib"
DEFAULT_CSV_PATH = Path("docs/input/library_20260729_190704.csv")
DEFAULT_COVER_JSON_PATH = Path("docs/input/biblioteca_ubemtem_com_capas_libib_final.json")
NOT_IDENTIFIED_NAME = "Not Identified"
NOT_IDENTIFIED_CODE = "not-identified"
UNCLASSIFIED_NAME = "Unclassified"
UNCLASSIFIED_CODE = "unclassified"


@dataclass
class ImportIssue:
    row: int
    field: str
    message: str


@dataclass
class ImportStats:
    rows_read: int = 0
    valid_rows: int = 0
    inserted_books: int = 0
    inserted_authors: int = 0
    discarded_duplicates: int = 0
    adjusted_titles: int = 0
    not_identified_authors: int = 0
    unclassified_genres: int = 0
    local_cover_found: int = 0
    local_cover_missing: int = 0
    google_books_cover_found: int = 0
    google_books_cover_missing: int = 0
    google_books_cover_errors: int = 0
    existing_covers_updated: int = 0
    invalid_rows: int = 0
    issues: list[ImportIssue] = field(default_factory=list)

    def add_issue(self, row: int, field_name: str, message: str) -> None:
        self.invalid_rows += 1
        self.issues.append(ImportIssue(row, field_name, message))


@dataclass(frozen=True)
class BookImportRow:
    row_number: int
    title: str
    normalized_title: str
    author_name: str
    normalized_author_name: str
    creator_credit: str | None
    isbn13: str | None
    isbn10: str | None
    description: str | None
    publisher: str | None
    published_on: date | None
    page_count: int | None
    copy_count: int
    cover_url: str | None
    collection_name: str | None
    source_added_on: date | None

    @property
    def primary_isbn(self) -> str | None:
        return self.isbn13 or self.isbn10


def main() -> int:
    args = parse_args()
    csv_path = Path(args.csv_path)
    stats = ImportStats()
    rows = read_csv_rows(csv_path, stats)

    if stats.issues:
        write_report(args.report, stats, dry_run=args.dry_run)
        print_report(stats, args.dry_run)
        return 1

    if args.fetch_covers:
        api_key = args.google_books_api_key or os.environ.get("GOOGLE_BOOKS_API_KEY")
        cover_index = load_local_cover_index(Path(args.cover_json_path))
        rows = enrich_cover_urls(rows, stats, cover_index=cover_index, google_books_api_key=api_key)

    if psycopg is None:
        print("psycopg is required. Install with: python3 -m pip install -r scripts/requirements.txt", file=sys.stderr)
        return 2

    database_url = args.database_url or os.environ.get("BOOKSLIB_DATABASE_URL") or DEFAULT_DATABASE_URL
    with psycopg.connect(database_url, row_factory=dict_row) as connection:
        try:
            import_rows(connection, rows, stats, publish_on_site=args.publish_on_site)
            if args.dry_run:
                connection.rollback()
            else:
                connection.commit()
        except Exception:
            connection.rollback()
            raise

    write_report(args.report, stats, dry_run=args.dry_run)
    print_report(stats, args.dry_run)
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Import the Books Library CSV export into PostgreSQL.")
    parser.add_argument("csv_path", nargs="?", default=str(DEFAULT_CSV_PATH), help="Path to the CSV export.")
    parser.add_argument("--database-url", help="PostgreSQL connection URL. Defaults to BOOKSLIB_DATABASE_URL or local Compose.")
    parser.add_argument("--dry-run", action="store_true", help="Validate and execute in a rolled-back transaction.")
    parser.add_argument("--publish-on-site", action="store_true", help="Import rows as publicly visible. Defaults to false.")
    parser.add_argument("--fetch-covers", action="store_true", help="Try to fill missing cover URLs from the local cover JSON, then Google Books.")
    parser.add_argument(
        "--cover-json-path",
        default=str(DEFAULT_COVER_JSON_PATH),
        help="Path to the enriched Libib JSON with cover_url values.",
    )
    parser.add_argument("--google-books-api-key", help="Google Books API key. Defaults to GOOGLE_BOOKS_API_KEY.")
    parser.add_argument("--report", help="Optional JSON report output path.")
    return parser.parse_args()


def read_csv_rows(path: Path, stats: ImportStats) -> list[BookImportRow]:
    rows: list[BookImportRow] = []
    with path.open("r", encoding="utf-8-sig", newline="") as csv_file:
        reader = csv.DictReader(csv_file)
        for row_number, raw in enumerate(reader, start=2):
            stats.rows_read += 1
            parsed = parse_row(row_number, raw, stats)
            if parsed is not None:
                stats.valid_rows += 1
                rows.append(parsed)

    return rows


@dataclass(frozen=True)
class LocalCoverIndex:
    by_isbn: dict[str, str]
    by_title: dict[str, str]


def enrich_cover_urls(
    rows: list[BookImportRow],
    stats: ImportStats,
    cover_index: LocalCoverIndex,
    google_books_api_key: str | None,
) -> list[BookImportRow]:
    google_cover_cache: dict[str, str | None] = {}
    enriched_rows: list[BookImportRow] = []

    for row in rows:
        if row.cover_url:
            enriched_rows.append(row)
            continue

        local_cover_url = find_local_cover_url(row, cover_index)
        if local_cover_url:
            stats.local_cover_found += 1
            enriched_rows.append(row_with_cover(row, local_cover_url))
            continue

        stats.local_cover_missing += 1

        if not row.primary_isbn or not google_books_api_key:
            enriched_rows.append(row)
            continue

        cover_url = google_cover_cache.get(row.primary_isbn)
        if row.primary_isbn not in google_cover_cache:
            try:
                cover_url = fetch_google_books_cover_url(row.primary_isbn, google_books_api_key)
            except urllib.error.URLError:
                stats.google_books_cover_errors += 1
                cover_url = None

            google_cover_cache[row.primary_isbn] = cover_url

        if cover_url:
            stats.google_books_cover_found += 1
            enriched_rows.append(row_with_cover(row, cover_url))
        else:
            stats.google_books_cover_missing += 1
            enriched_rows.append(row)

    return enriched_rows


def load_local_cover_index(path: Path) -> LocalCoverIndex:
    if not path.exists():
        return LocalCoverIndex(by_isbn={}, by_title={})

    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    by_isbn: dict[str, str] = {}
    by_title: dict[str, str] = {}

    for item in payload:
        cover_url = text(item.get("cover_url"))
        if not cover_url:
            continue

        isbn13 = normalize_isbn13(item.get("ean_isbn13"))
        isbn10 = normalize_isbn10(item.get("upc_isbn10"))
        for isbn in (isbn13, isbn10):
            if isbn:
                by_isbn.setdefault(isbn, cover_url)

        for title_key in ("title", "libib_title"):
            title = text(item.get(title_key))
            if title:
                by_title.setdefault(normalize_key(title), cover_url)

    return LocalCoverIndex(by_isbn=by_isbn, by_title=by_title)


def find_local_cover_url(row: BookImportRow, cover_index: LocalCoverIndex) -> str | None:
    for isbn in (row.isbn13, row.isbn10):
        if isbn and isbn in cover_index.by_isbn:
            return cover_index.by_isbn[isbn]

    return cover_index.by_title.get(row.normalized_title)


def fetch_google_books_cover_url(isbn: str, api_key: str) -> str | None:
    query = urllib.parse.urlencode(
        {
            "q": f"isbn:{isbn}",
            "fields": "items(volumeInfo/imageLinks)",
            "key": api_key,
        }
    )
    request = urllib.request.Request(
        f"https://www.googleapis.com/books/v1/volumes?{query}",
        headers={"Accept": "application/json"},
    )

    with urllib.request.urlopen(request, timeout=8) as response:
        payload = json.loads(response.read().decode("utf-8"))

    return select_google_books_cover_url(payload)


def select_google_books_cover_url(payload: dict[str, Any]) -> str | None:
    for item in payload.get("items", []):
        image_links = item.get("volumeInfo", {}).get("imageLinks", {})
        for key in ("thumbnail", "smallThumbnail"):
            cover_url = text(image_links.get(key))
            if cover_url:
                return force_https(cover_url)

    return None


def force_https(value: str) -> str:
    if value.startswith("http://"):
        return f"https://{value.removeprefix('http://')}"

    return value


def row_with_cover(row: BookImportRow, cover_url: str) -> BookImportRow:
    return BookImportRow(
        row_number=row.row_number,
        title=row.title,
        normalized_title=row.normalized_title,
        author_name=row.author_name,
        normalized_author_name=row.normalized_author_name,
        creator_credit=row.creator_credit,
        isbn13=row.isbn13,
        isbn10=row.isbn10,
        description=row.description,
        publisher=row.publisher,
        published_on=row.published_on,
        page_count=row.page_count,
        copy_count=row.copy_count,
        cover_url=cover_url,
        collection_name=row.collection_name,
        source_added_on=row.source_added_on,
    )


def parse_row(row_number: int, raw: dict[str, str | None], stats: ImportStats) -> BookImportRow | None:
    if text(raw.get("item_type")) not in (None, "book"):
        stats.add_issue(row_number, "item_type", "Only book rows can be imported.")
        return None

    title = text(raw.get("title"))
    if title is None:
        stats.add_issue(row_number, "title", "Title is required.")
        return None

    author_name = resolve_author_name(raw)
    if author_name == NOT_IDENTIFIED_NAME:
        stats.not_identified_authors += 1

    isbn13 = normalize_isbn13(raw.get("ean_isbn13"))
    isbn10 = normalize_isbn10(raw.get("upc_isbn10"))
    published_on = parse_date(raw.get("publish_date"), row_number, "publish_date", stats)
    source_added_on = parse_date(raw.get("added"), row_number, "added", stats)
    page_count = parse_positive_int(raw.get("length"), row_number, "length", stats, allow_empty=True)
    copy_count = parse_positive_int(raw.get("copies"), row_number, "copies", stats, allow_empty=False) or 1
    cover_url = text(raw.get("cover_url") or raw.get("image_url") or raw.get("cover"))

    validate_lengths(
        row_number,
        stats,
        {
            "title": (title, 240),
            "author": (author_name, 160),
            "creators": (text(raw.get("creators")), 500),
            "description": (text(raw.get("description")), 4000),
            "publisher": (text(raw.get("publisher")), 240),
            "collection": (text(raw.get("collection")), 240),
            "cover_url": (cover_url, 1000),
        },
    )

    if isbn13 is not None and len(isbn13) != 13:
        stats.add_issue(row_number, "ean_isbn13", "ISBN-13 must contain exactly 13 digits.")

    if isbn10 is not None and not re.fullmatch(r"\d{9}[\dX]", isbn10):
        stats.add_issue(row_number, "upc_isbn10", "ISBN-10 must contain exactly 10 digits or 9 digits followed by X.")

    if stats.issues and stats.issues[-1].row == row_number:
        return None

    return BookImportRow(
        row_number=row_number,
        title=title,
        normalized_title=normalize_key(title),
        author_name=author_name,
        normalized_author_name=normalize_key(author_name),
        creator_credit=text(raw.get("creators")),
        isbn13=isbn13,
        isbn10=isbn10,
        description=text(raw.get("description")),
        publisher=text(raw.get("publisher")),
        published_on=published_on,
        page_count=page_count,
        copy_count=copy_count,
        cover_url=cover_url,
        collection_name=text(raw.get("collection")),
        source_added_on=source_added_on,
    )


def import_rows(connection: Any, rows: list[BookImportRow], stats: ImportStats, publish_on_site: bool) -> None:
    author_ids = load_author_ids(connection)
    title_isbns, existing_isbns = load_existing_title_isbns(connection)
    used_titles = set(title_isbns)
    genre_id = ensure_genre(connection)
    stats.unclassified_genres = len(rows)

    for row in rows:
        if is_duplicate_same_isbn(row, title_isbns, existing_isbns):
            if row.cover_url:
                stats.existing_covers_updated += update_existing_cover_url(connection, row)
            stats.discarded_duplicates += 1
            continue

        title = row.title
        normalized_title = row.normalized_title
        if normalized_title in used_titles:
            title, normalized_title = make_edition_title(title, used_titles)
            stats.adjusted_titles += 1

        author_id = author_ids.get(row.normalized_author_name)
        if author_id is None:
            author_id = insert_author(connection, row.author_name, row.normalized_author_name)
            author_ids[row.normalized_author_name] = author_id
            stats.inserted_authors += 1

        insert_book(connection, row, title, normalized_title, author_id, genre_id, publish_on_site)
        stats.inserted_books += 1
        used_titles.add(normalized_title)
        imported_isbns = {isbn for isbn in [row.isbn13, row.isbn10] if isbn}
        title_isbns.setdefault(normalized_title, set()).update(imported_isbns)
        existing_isbns.update(imported_isbns)


def ensure_genre(connection: Any) -> uuid.UUID:
    existing = connection.execute(
        "select id from genres where system_code = %s and deleted_at_utc is null",
        (UNCLASSIFIED_CODE,),
    ).fetchone()
    if existing:
        return existing["id"]

    genre_id = uuid.uuid4()
    connection.execute(
        """
        insert into genres (id, name, normalized_name, system_code, created_at_utc)
        values (%s, %s, %s, %s, %s)
        on conflict do nothing
        """,
        (genre_id, UNCLASSIFIED_NAME, normalize_key(UNCLASSIFIED_NAME), UNCLASSIFIED_CODE, utc_now()),
    )
    return connection.execute(
        "select id from genres where system_code = %s and deleted_at_utc is null",
        (UNCLASSIFIED_CODE,),
    ).fetchone()["id"]


def load_author_ids(connection: Any) -> dict[str, uuid.UUID]:
    ensure_not_identified_author(connection)
    rows = connection.execute(
        "select id, normalized_name from authors where deleted_at_utc is null"
    ).fetchall()
    return {row["normalized_name"]: row["id"] for row in rows}


def ensure_not_identified_author(connection: Any) -> None:
    if connection.execute("select 1 from authors where system_code = %s", (NOT_IDENTIFIED_CODE,)).fetchone():
        return

    connection.execute(
        """
        insert into authors (id, name, normalized_name, system_code, created_at_utc)
        values (%s, %s, %s, %s, %s)
        on conflict do nothing
        """,
        (uuid.uuid4(), NOT_IDENTIFIED_NAME, normalize_key(NOT_IDENTIFIED_NAME), NOT_IDENTIFIED_CODE, utc_now()),
    )


def load_existing_title_isbns(connection: Any) -> tuple[dict[str, set[str]], set[str]]:
    rows = connection.execute(
        """
        select normalized_title, isbn13, isbn10
        from books
        where deleted_at_utc is null
        """
    ).fetchall()
    title_isbns: dict[str, set[str]] = {}
    existing_isbns: set[str] = set()
    for row in rows:
        row_isbns = {isbn for isbn in [row["isbn13"], row["isbn10"]] if isbn}
        title_isbns.setdefault(row["normalized_title"], set()).update(row_isbns)
        existing_isbns.update(row_isbns)
    return title_isbns, existing_isbns


def is_duplicate_same_isbn(row: BookImportRow, title_isbns: dict[str, set[str]], existing_isbns: set[str]) -> bool:
    title_existing_isbns = title_isbns.get(row.normalized_title, set())
    if row.primary_isbn is None:
        return row.normalized_title in title_isbns

    return row.primary_isbn in title_existing_isbns or row.primary_isbn in existing_isbns


def make_edition_title(title: str, used_titles: set[str]) -> tuple[str, str]:
    dots = 1
    while True:
        suffix = "." * dots
        candidate = f"{title[:240 - dots]}{suffix}"
        normalized = normalize_key(candidate)
        if normalized not in used_titles:
            return candidate, normalized
        dots += 1


def insert_author(connection: Any, name: str, normalized_name: str) -> uuid.UUID:
    author_id = uuid.uuid4()
    connection.execute(
        """
        insert into authors (id, name, normalized_name, created_at_utc)
        values (%s, %s, %s, %s)
        """,
        (author_id, name, normalized_name, utc_now()),
    )
    return author_id


def insert_book(
    connection: Any,
    row: BookImportRow,
    title: str,
    normalized_title: str,
    author_id: uuid.UUID,
    genre_id: uuid.UUID,
    publish_on_site: bool,
) -> None:
    connection.execute(
        """
        insert into books (
            id, title, normalized_title, author_id, genre_id, creator_credit,
            isbn13, isbn10, description, publisher, published_on, page_count,
            copy_count, cover_url, collection_name, source_added_on,
            publish_on_site, created_at_utc
        )
        values (
            %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s,
            %s, %s
        )
        """,
        (
            uuid.uuid4(),
            title,
            normalized_title,
            author_id,
            genre_id,
            row.creator_credit,
            row.isbn13,
            row.isbn10,
            row.description,
            row.publisher,
            row.published_on,
            row.page_count,
            row.copy_count,
            row.cover_url,
            row.collection_name,
            row.source_added_on,
            publish_on_site,
            utc_now(),
        ),
    )


def update_existing_cover_url(connection: Any, row: BookImportRow) -> int:
    match_clauses: list[str] = []
    match_values: list[str] = []

    if row.isbn13:
        match_clauses.append("isbn13 = %s")
        match_values.append(row.isbn13)

    if row.isbn10:
        match_clauses.append("isbn10 = %s")
        match_values.append(row.isbn10)

    if not match_clauses:
        match_clauses.append("normalized_title = %s")
        match_values.append(row.normalized_title)

    cursor = connection.execute(
        f"""
        update books
        set cover_url = %s, updated_at_utc = %s
        where deleted_at_utc is null
          and cover_url is null
          and ({" or ".join(match_clauses)})
        """,
        (row.cover_url, utc_now(), *match_values),
    )
    return cursor.rowcount


def resolve_author_name(raw: dict[str, str | None]) -> str:
    libib_author = text(raw.get("libib_author"))
    if libib_author:
        return libib_author

    first_name = text(raw.get("first_name"))
    last_name = text(raw.get("last_name"))
    if first_name or last_name:
        return " ".join(value for value in [first_name, last_name] if value)

    return text(raw.get("creators")) or NOT_IDENTIFIED_NAME


def validate_lengths(row_number: int, stats: ImportStats, fields: dict[str, tuple[str | None, int]]) -> None:
    for field_name, (value, limit) in fields.items():
        if value is not None and len(value) > limit:
            stats.add_issue(row_number, field_name, f"Value must be {limit} characters or fewer.")


def parse_date(value: str | None, row_number: int, field_name: str, stats: ImportStats) -> date | None:
    normalized = text(value)
    if normalized is None:
        return None

    try:
        return date.fromisoformat(normalized)
    except ValueError:
        stats.add_issue(row_number, field_name, "Date must use YYYY-MM-DD format.")
        return None


def parse_positive_int(value: str | None, row_number: int, field_name: str, stats: ImportStats, allow_empty: bool) -> int | None:
    normalized = text(value)
    if normalized is None:
        if not allow_empty:
            stats.add_issue(row_number, field_name, "Value is required.")
        return None

    try:
        parsed = int(normalized)
    except ValueError:
        stats.add_issue(row_number, field_name, "Value must be a positive integer.")
        return None

    if parsed < 1:
        stats.add_issue(row_number, field_name, "Value must be at least 1.")
        return None

    return parsed


def normalize_isbn13(value: str | None) -> str | None:
    normalized = text(value)
    if normalized is None:
        return None

    return re.sub(r"\D", "", normalized) or None


def normalize_isbn10(value: str | None) -> str | None:
    normalized = text(value)
    if normalized is None:
        return None

    return re.sub(r"[^0-9Xx]", "", normalized).upper() or None


def text(value: str | None) -> str | None:
    if value is None:
        return None

    normalized = value.strip()
    return normalized or None


def normalize_key(value: str) -> str:
    return value.strip().upper()


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def write_report(path: str | None, stats: ImportStats, dry_run: bool) -> None:
    if path is None:
        return

    report = asdict(stats)
    report["dry_run"] = dry_run
    Path(path).write_text(json.dumps(report, indent=2, default=str) + "\n", encoding="utf-8")


def print_report(stats: ImportStats, dry_run: bool) -> None:
    mode = "dry-run" if dry_run else "import"
    print(f"Catalog CSV {mode} report")
    print(f"Rows read: {stats.rows_read}")
    print(f"Valid rows: {stats.valid_rows}")
    print(f"Inserted books: {stats.inserted_books}")
    print(f"Inserted authors: {stats.inserted_authors}")
    print(f"Discarded duplicates: {stats.discarded_duplicates}")
    print(f"Adjusted duplicate titles: {stats.adjusted_titles}")
    print(f"Not Identified author fallbacks: {stats.not_identified_authors}")
    print(f"Unclassified genre assignments: {stats.unclassified_genres}")
    print(f"Local JSON covers found: {stats.local_cover_found}")
    print(f"Local JSON covers missing: {stats.local_cover_missing}")
    print(f"Google Books covers found: {stats.google_books_cover_found}")
    print(f"Google Books covers missing: {stats.google_books_cover_missing}")
    print(f"Google Books cover lookup errors: {stats.google_books_cover_errors}")
    print(f"Existing covers updated: {stats.existing_covers_updated}")
    print(f"Invalid rows: {stats.invalid_rows}")

    if stats.issues:
        print("Issues:")
        for issue in stats.issues[:50]:
            print(f"- row {issue.row}, {issue.field}: {issue.message}")
        if len(stats.issues) > 50:
            print(f"- ... {len(stats.issues) - 50} more issue(s)")


if __name__ == "__main__":
    raise SystemExit(main())
