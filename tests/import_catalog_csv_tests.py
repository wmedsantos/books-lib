import importlib.util
import sys
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "scripts" / "import_catalog_csv.py"
SPEC = importlib.util.spec_from_file_location("import_catalog_csv", SCRIPT_PATH)
import_catalog_csv = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules["import_catalog_csv"] = import_catalog_csv
SPEC.loader.exec_module(import_catalog_csv)


class ImportCatalogCsvTests(unittest.TestCase):
    def test_resolves_author_from_first_and_last_name(self):
        author = import_catalog_csv.resolve_author_name(
            {
                "creators": "Cintia Barreto, Luciana Grether",
                "first_name": "Cintia",
                "last_name": "Barreto",
            }
        )

        self.assertEqual("Cintia Barreto", author)

    def test_resolves_not_identified_when_author_fields_are_missing(self):
        author = import_catalog_csv.resolve_author_name(
            {
                "creators": "",
                "first_name": "",
                "last_name": "",
            }
        )

        self.assertEqual("Not Identified", author)

    def test_discards_same_title_same_isbn(self):
        row = import_catalog_csv.BookImportRow(
            row_number=2,
            title="Fala, menina!",
            normalized_title="FALA, MENINA!",
            author_name="Cintia Barreto",
            normalized_author_name="CINTIA BARRETO",
            creator_credit=None,
            isbn13="9788595000803",
            isbn10=None,
            description=None,
            publisher=None,
            published_on=None,
            page_count=None,
            copy_count=1,
            cover_url=None,
            collection_name=None,
            source_added_on=None,
        )

        self.assertTrue(
            import_catalog_csv.is_duplicate_same_isbn(
                row,
                {"FALA, MENINA!": {"9788595000803"}},
                {"9788595000803"},
            )
        )

    def test_discards_existing_isbn_even_when_title_was_adjusted(self):
        row = import_catalog_csv.BookImportRow(
            row_number=2,
            title="O Tupi Que Você Fala",
            normalized_title="O TUPI QUE VOCÊ FALA",
            author_name="Claudio Fragata",
            normalized_author_name="CLAUDIO FRAGATA",
            creator_credit=None,
            isbn13="9786585206337",
            isbn10=None,
            description=None,
            publisher=None,
            published_on=None,
            page_count=None,
            copy_count=1,
            cover_url=None,
            collection_name=None,
            source_added_on=None,
        )

        self.assertTrue(
            import_catalog_csv.is_duplicate_same_isbn(
                row,
                {"O TUPI QUE VOCÊ FALA": {"9788566642139"}, "O TUPI QUE VOCÊ FALA.": {"9786585206337"}},
                {"9788566642139", "9786585206337"},
            )
        )

    def test_discards_same_title_without_isbn_for_rerunnable_import(self):
        row = import_catalog_csv.BookImportRow(
            row_number=2,
            title="Sem ISBN",
            normalized_title="SEM ISBN",
            author_name="Not Identified",
            normalized_author_name="NOT IDENTIFIED",
            creator_credit=None,
            isbn13=None,
            isbn10=None,
            description=None,
            publisher=None,
            published_on=None,
            page_count=None,
            copy_count=1,
            cover_url=None,
            collection_name=None,
            source_added_on=None,
        )

        self.assertTrue(import_catalog_csv.is_duplicate_same_isbn(row, {"SEM ISBN": set()}, set()))

    def test_adds_period_for_same_title_different_isbn(self):
        title, normalized = import_catalog_csv.make_edition_title("Fala, menina!", {"FALA, MENINA!"})

        self.assertEqual("Fala, menina!.", title)
        self.assertEqual("FALA, MENINA!.", normalized)

    def test_parse_real_csv_fixture(self):
        stats = import_catalog_csv.ImportStats()
        rows = import_catalog_csv.read_csv_rows(Path("docs/input/library_20260729_190704.csv"), stats)

        self.assertEqual(105, stats.rows_read)
        self.assertGreater(len(rows), 100)
        self.assertEqual(0, stats.invalid_rows)

    def test_local_cover_index_matches_by_isbn_first(self):
        index = import_catalog_csv.LocalCoverIndex(
            by_isbn={"9788595000803": "https://example.com/isbn.jpg"},
            by_title={"FALA, MENINA!": "https://example.com/title.jpg"},
        )
        row = import_catalog_csv.BookImportRow(
            row_number=2,
            title="Fala, menina!",
            normalized_title="FALA, MENINA!",
            author_name="Cintia Barreto",
            normalized_author_name="CINTIA BARRETO",
            creator_credit=None,
            isbn13="9788595000803",
            isbn10=None,
            description=None,
            publisher=None,
            published_on=None,
            page_count=None,
            copy_count=1,
            cover_url=None,
            collection_name=None,
            source_added_on=None,
        )

        self.assertEqual("https://example.com/isbn.jpg", import_catalog_csv.find_local_cover_url(row, index))

    def test_local_cover_index_falls_back_to_title(self):
        index = import_catalog_csv.LocalCoverIndex(
            by_isbn={},
            by_title={"FALA, MENINA!": "https://example.com/title.jpg"},
        )
        row = import_catalog_csv.BookImportRow(
            row_number=2,
            title="Fala, menina!",
            normalized_title="FALA, MENINA!",
            author_name="Cintia Barreto",
            normalized_author_name="CINTIA BARRETO",
            creator_credit=None,
            isbn13=None,
            isbn10=None,
            description=None,
            publisher=None,
            published_on=None,
            page_count=None,
            copy_count=1,
            cover_url=None,
            collection_name=None,
            source_added_on=None,
        )

        self.assertEqual("https://example.com/title.jpg", import_catalog_csv.find_local_cover_url(row, index))

    def test_loads_real_local_cover_json(self):
        index = import_catalog_csv.load_local_cover_index(Path("docs/input/biblioteca_ubemtem_com_capas_libib_final.json"))

        self.assertGreater(len(index.by_isbn), 100)
        self.assertGreater(len(index.by_title), 100)

    def test_select_google_books_cover_prefers_thumbnail_and_forces_https(self):
        cover_url = import_catalog_csv.select_google_books_cover_url(
            {
                "items": [
                    {
                        "volumeInfo": {
                            "imageLinks": {
                                "smallThumbnail": "http://example.com/small.jpg",
                                "thumbnail": "http://example.com/thumb.jpg",
                            }
                        }
                    }
                ]
            }
        )

        self.assertEqual("https://example.com/thumb.jpg", cover_url)

    def test_select_google_books_cover_returns_none_when_missing(self):
        self.assertIsNone(import_catalog_csv.select_google_books_cover_url({"items": [{"volumeInfo": {}}]}))


if __name__ == "__main__":
    unittest.main()
