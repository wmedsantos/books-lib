import { FormEvent, useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import './App.css'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080',
})

type Tab = 'books' | 'authors' | 'genres'

type ReferenceRecord = {
  id: string
  name: string
  systemCode: string | null
  isSystem: boolean
}

type ReferenceListResponse = {
  items: ReferenceRecord[]
  page: number
  pageSize: number
  total: number
}

type Book = {
  id: string
  title: string
  authorId: string
  authorName: string
  genreId: string
  genreName: string
  creatorCredit: string | null
  isbn13: string | null
  isbn10: string | null
  description: string | null
  publisher: string | null
  publishedOn: string | null
  pageCount: number | null
  copyCount: number
  coverUrl: string | null
  collectionName: string | null
  sourceAddedOn: string | null
  publishOnSite: boolean
}

type BookListResponse = {
  items: Book[]
  page: number
  pageSize: number
  total: number
}

type BookForm = {
  title: string
  authorId: string
  genreId: string
  creatorCredit: string
  isbn13: string
  isbn10: string
  description: string
  publisher: string
  publishedOn: string
  pageCount: string
  copyCount: string
  coverUrl: string
  collectionName: string
  sourceAddedOn: string
  publishOnSite: boolean
}

type ValidationErrors = Record<string, string[]>

type Session = {
  accessToken: string
  email: string
  passwordChangeRequired: boolean
}

const emptyBookForm: BookForm = {
  title: '',
  authorId: '',
  genreId: '',
  creatorCredit: '',
  isbn13: '',
  isbn10: '',
  description: '',
  publisher: '',
  publishedOn: '',
  pageCount: '',
  copyCount: '1',
  coverUrl: '',
  collectionName: '',
  sourceAddedOn: '',
  publishOnSite: false,
}

function App() {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<Session | null>(() => readSession())
  const [loginEmail, setLoginEmail] = useState('admin@bookslib.local')
  const [loginPassword, setLoginPassword] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [tab, setTab] = useState<Tab>('books')
  const [bookSearch, setBookSearch] = useState('')
  const [bookAuthorFilter, setBookAuthorFilter] = useState('')
  const [bookGenreFilter, setBookGenreFilter] = useState('')
  const [referenceSearch, setReferenceSearch] = useState('')
  const [bookForm, setBookForm] = useState<BookForm>(emptyBookForm)
  const [editingBookId, setEditingBookId] = useState<string | null>(null)
  const [referenceName, setReferenceName] = useState('')
  const [editingReference, setEditingReference] = useState<ReferenceRecord | null>(null)
  const [editingReferenceName, setEditingReferenceName] = useState('')

  useEffect(() => {
    if (session) {
      api.defaults.headers.common.Authorization = `Bearer ${session.accessToken}`
      localStorage.setItem('books-lib-session', JSON.stringify(session))
    } else {
      delete api.defaults.headers.common.Authorization
      localStorage.removeItem('books-lib-session')
    }
  }, [session])

  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: async () => {
      const response = await api.get<string>('/health/ready', { responseType: 'text' })
      return response.data
    },
    refetchInterval: 15000,
  })

  const authorsQuery = useQuery({
    queryKey: ['authors', 'all'],
    queryFn: () => fetchReferences('authors', '', 100),
  })

  const genresQuery = useQuery({
    queryKey: ['genres', 'all'],
    queryFn: () => fetchReferences('genres', '', 100),
  })

  const referenceQuery = useQuery({
    queryKey: [tab, referenceSearch],
    queryFn: () => fetchReferences(tab, referenceSearch, 50),
    enabled: tab === 'authors' || tab === 'genres',
  })

  const booksQuery = useQuery({
    queryKey: ['books', bookSearch, bookAuthorFilter, bookGenreFilter],
    queryFn: async () => {
      const response = await api.get<BookListResponse>('/api/v1/books', {
        params: {
          search: bookSearch || undefined,
          authorId: bookAuthorFilter || undefined,
          genreId: bookGenreFilter || undefined,
          pageSize: 50,
        },
      })
      return response.data
    },
  })

  const createBook = useMutation({
    mutationFn: async (payload: BookForm) => {
      const response = await api.post<Book>('/api/v1/books', toBookPayload(payload))
      return response.data
    },
    onSuccess: () => {
      setBookForm(emptyBookForm)
      queryClient.invalidateQueries({ queryKey: ['books'] })
    },
  })

  const updateBook = useMutation({
    mutationFn: async (payload: { id: string; form: BookForm }) => {
      const response = await api.put<Book>(`/api/v1/books/${payload.id}`, toBookPayload(payload.form))
      return response.data
    },
    onSuccess: () => {
      cancelBookEdit()
      queryClient.invalidateQueries({ queryKey: ['books'] })
    },
  })

  const deleteBook = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/v1/books/${id}`)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['books'] }),
  })

  const createReference = useMutation({
    mutationFn: async (payload: { type: 'authors' | 'genres'; name: string }) => {
      const response = await api.post<ReferenceRecord>(`/api/v1/${payload.type}`, { name: payload.name })
      return response.data
    },
    onSuccess: (_, variables) => {
      setReferenceName('')
      queryClient.invalidateQueries({ queryKey: [variables.type] })
      queryClient.invalidateQueries({ queryKey: [variables.type, 'all'] })
    },
  })

  const updateReference = useMutation({
    mutationFn: async (payload: { type: 'authors' | 'genres'; id: string; name: string }) => {
      const response = await api.put<ReferenceRecord>(`/api/v1/${payload.type}/${payload.id}`, { name: payload.name })
      return response.data
    },
    onSuccess: (_, variables) => {
      setEditingReference(null)
      setEditingReferenceName('')
      queryClient.invalidateQueries({ queryKey: [variables.type] })
      queryClient.invalidateQueries({ queryKey: [variables.type, 'all'] })
      queryClient.invalidateQueries({ queryKey: ['books'] })
    },
  })

  const deleteReference = useMutation({
    mutationFn: async (payload: { type: 'authors' | 'genres'; id: string }) => {
      await api.delete(`/api/v1/${payload.type}/${payload.id}`)
      return payload
    },
    onSuccess: (variables) => {
      queryClient.invalidateQueries({ queryKey: [variables.type] })
      queryClient.invalidateQueries({ queryKey: [variables.type, 'all'] })
    },
  })

  const login = useMutation({
    mutationFn: async () => {
      const response = await api.post<Session>('/api/v1/identity/login', {
        email: loginEmail,
        password: loginPassword,
      })
      return response.data
    },
    onSuccess: (data) => {
      setSession(data)
      setLoginPassword('')
    },
  })

  const changePassword = useMutation({
    mutationFn: async () => {
      const response = await api.post<Session>('/api/v1/identity/change-password', {
        currentPassword,
        newPassword,
      })
      return response.data
    },
    onSuccess: (data) => {
      setSession(data)
      setCurrentPassword('')
      setNewPassword('')
    },
  })

  const healthLabel = useMemo(() => {
    if (healthQuery.isPending) return 'checking'
    if (healthQuery.isError) return 'offline'
    return healthQuery.data?.toLowerCase() === 'healthy' ? 'ready' : 'degraded'
  }, [healthQuery.data, healthQuery.isError, healthQuery.isPending])

  const authors = authorsQuery.data?.items ?? []
  const genres = genresQuery.data?.items ?? []
  const referenceType = tab === 'authors' ? 'authors' : 'genres'
  const bookMutationError = createBook.isError ? createBook.error : updateBook.isError ? updateBook.error : null
  const bookErrors = getValidationErrors(bookMutationError)
  const loginErrors = getValidationErrors(login.error)
  const changePasswordErrors = getValidationErrors(changePassword.error)
  const referenceMutationError = editingReference
    ? updateReference.isError ? updateReference.error : null
    : createReference.isError ? createReference.error : null
  const referenceErrors = getValidationErrors(referenceMutationError)

  function submitBook(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (editingBookId) {
      updateBook.mutate({ id: editingBookId, form: bookForm })
    } else {
      createBook.mutate(bookForm)
    }
  }

  function editBook(book: Book) {
    setEditingBookId(book.id)
    setBookForm({
      title: book.title,
      authorId: book.authorId,
      genreId: book.genreId,
      creatorCredit: book.creatorCredit ?? '',
      isbn13: book.isbn13 ?? '',
      isbn10: book.isbn10 ?? '',
      description: book.description ?? '',
      publisher: book.publisher ?? '',
      publishedOn: book.publishedOn ?? '',
      pageCount: book.pageCount?.toString() ?? '',
      copyCount: book.copyCount.toString(),
      coverUrl: book.coverUrl ?? '',
      collectionName: book.collectionName ?? '',
      sourceAddedOn: book.sourceAddedOn ?? '',
      publishOnSite: book.publishOnSite,
    })
    setTab('books')
  }

  function cancelBookEdit() {
    setEditingBookId(null)
    setBookForm(emptyBookForm)
  }

  function submitReference(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    createReference.mutate({ type: referenceType, name: referenceName })
  }

  function submitReferenceEdit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (editingReference) {
      updateReference.mutate({ type: referenceType, id: editingReference.id, name: editingReferenceName })
    }
  }

  function startReferenceEdit(record: ReferenceRecord) {
    setEditingReference(record)
    setEditingReferenceName(record.name)
  }

  if (!session) {
    return (
      <AuthShell healthLabel={healthLabel}>
        <form
          className="auth-card form-stack"
          onSubmit={(event) => {
            event.preventDefault()
            login.mutate()
          }}
        >
          <h1>Catalog Sign In</h1>
          <label htmlFor="login-email">Email</label>
          <input
            id="login-email"
            className={fieldClass(loginErrors, 'email')}
            aria-invalid={hasFieldError(loginErrors, 'email')}
            value={loginEmail}
            onChange={(event) => setLoginEmail(event.target.value)}
          />
          <FieldMessages errors={loginErrors} field="email" />

          <label htmlFor="login-password">Password</label>
          <input
            id="login-password"
            className={fieldClass(loginErrors, 'password')}
            aria-invalid={hasFieldError(loginErrors, 'password')}
            type="password"
            value={loginPassword}
            onChange={(event) => setLoginPassword(event.target.value)}
          />
          <FieldMessages errors={loginErrors} field="password" />

          {login.isError && <ProblemMessage error={login.error} />}
          <button type="submit" disabled={login.isPending}>
            Sign In
          </button>
        </form>
      </AuthShell>
    )
  }

  if (session.passwordChangeRequired) {
    return (
      <AuthShell healthLabel={healthLabel}>
        <form
          className="auth-card form-stack"
          onSubmit={(event) => {
            event.preventDefault()
            changePassword.mutate()
          }}
        >
          <h1>Change Password</h1>
          <p className="muted">{session.email}</p>
          <label htmlFor="current-password">Current Password</label>
          <input
            id="current-password"
            className={fieldClass(changePasswordErrors, 'currentPassword')}
            aria-invalid={hasFieldError(changePasswordErrors, 'currentPassword')}
            type="password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
          />
          <FieldMessages errors={changePasswordErrors} field="currentPassword" />

          <label htmlFor="new-password">New Password</label>
          <input
            id="new-password"
            className={fieldClass(changePasswordErrors, 'newPassword')}
            aria-invalid={hasFieldError(changePasswordErrors, 'newPassword')}
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
          />
          <FieldMessages errors={changePasswordErrors} field="newPassword" />

          {changePassword.isError && <ProblemMessage error={changePassword.error} />}
          <div className="actions">
            <button type="submit" disabled={changePassword.isPending}>
              Save Password
            </button>
            <button type="button" className="secondary" onClick={() => setSession(null)}>
              Sign Out
            </button>
          </div>
        </form>
      </AuthShell>
    )
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Books Library</p>
          <h1>Catalog Administration</h1>
        </div>
        <div className={`health health-${healthLabel}`}>
          <span aria-hidden="true" />
          API {healthLabel}
        </div>
        <button type="button" className="secondary" onClick={() => setSession(null)}>
          Sign Out
        </button>
      </header>

      <nav className="tabs" aria-label="Catalog sections">
        <button type="button" className={tab === 'books' ? 'active' : ''} onClick={() => setTab('books')}>
          Books
        </button>
        <button type="button" className={tab === 'authors' ? 'active' : ''} onClick={() => setTab('authors')}>
          Authors
        </button>
        <button type="button" className={tab === 'genres' ? 'active' : ''} onClick={() => setTab('genres')}>
          Genres
        </button>
      </nav>

      {tab === 'books' ? (
        <section className="workspace book-workspace" aria-label="Book management">
          <aside className="panel">
            <h2>{editingBookId ? 'Edit Book' : 'New Book'}</h2>
            <form onSubmit={submitBook} className="form-stack">
              <label htmlFor="book-title">Title</label>
              <input
                id="book-title"
                className={fieldClass(bookErrors, 'title')}
                aria-invalid={hasFieldError(bookErrors, 'title')}
                value={bookForm.title}
                onChange={(event) => setBookFormField('title', event.target.value)}
                maxLength={240}
              />
              <FieldMessages errors={bookErrors} field="title" />

              <label htmlFor="book-author">Author</label>
              <select
                id="book-author"
                className={fieldClass(bookErrors, 'authorId')}
                aria-invalid={hasFieldError(bookErrors, 'authorId')}
                value={bookForm.authorId}
                onChange={(event) => setBookFormField('authorId', event.target.value)}
              >
                <option value="">Select author</option>
                {authors.map((author) => (
                  <option key={author.id} value={author.id}>
                    {author.name}
                  </option>
                ))}
              </select>
              <FieldMessages errors={bookErrors} field="authorId" />

              <label htmlFor="book-genre">Genre</label>
              <select
                id="book-genre"
                className={fieldClass(bookErrors, 'genreId')}
                aria-invalid={hasFieldError(bookErrors, 'genreId')}
                value={bookForm.genreId}
                onChange={(event) => setBookFormField('genreId', event.target.value)}
              >
                <option value="">Select genre</option>
                {genres.map((genre) => (
                  <option key={genre.id} value={genre.id}>
                    {genre.name}
                  </option>
                ))}
              </select>
              <FieldMessages errors={bookErrors} field="genreId" />

              <label htmlFor="book-copies">Copies</label>
              <input
                id="book-copies"
                className={fieldClass(bookErrors, 'copyCount')}
                aria-invalid={hasFieldError(bookErrors, 'copyCount')}
                type="number"
                min="1"
                value={bookForm.copyCount}
                onChange={(event) => setBookFormField('copyCount', event.target.value)}
              />
              <FieldMessages errors={bookErrors} field="copyCount" />

              <div className="form-grid">
                <label>
                  <span>ISBN-13</span>
                  <input
                    className={fieldClass(bookErrors, 'isbn13')}
                    aria-invalid={hasFieldError(bookErrors, 'isbn13')}
                    value={bookForm.isbn13}
                    onChange={(event) => setBookFormField('isbn13', event.target.value)}
                    maxLength={13}
                  />
                  <FieldMessages errors={bookErrors} field="isbn13" />
                </label>
                <label>
                  <span>ISBN-10</span>
                  <input
                    className={fieldClass(bookErrors, 'isbn10')}
                    aria-invalid={hasFieldError(bookErrors, 'isbn10')}
                    value={bookForm.isbn10}
                    onChange={(event) => setBookFormField('isbn10', event.target.value)}
                    maxLength={10}
                  />
                  <FieldMessages errors={bookErrors} field="isbn10" />
                </label>
              </div>

              <div className="form-grid">
                <label>
                  <span>Publisher</span>
                  <input
                    className={fieldClass(bookErrors, 'publisher')}
                    aria-invalid={hasFieldError(bookErrors, 'publisher')}
                    value={bookForm.publisher}
                    onChange={(event) => setBookFormField('publisher', event.target.value)}
                  />
                  <FieldMessages errors={bookErrors} field="publisher" />
                </label>
                <label>
                  <span>Published</span>
                  <input type="date" value={bookForm.publishedOn} onChange={(event) => setBookFormField('publishedOn', event.target.value)} />
                </label>
              </div>

              <div className="form-grid">
                <label>
                  <span>Page Count</span>
                  <input
                    className={fieldClass(bookErrors, 'pageCount')}
                    aria-invalid={hasFieldError(bookErrors, 'pageCount')}
                    type="number"
                    min="1"
                    value={bookForm.pageCount}
                    onChange={(event) => setBookFormField('pageCount', event.target.value)}
                  />
                  <FieldMessages errors={bookErrors} field="pageCount" />
                </label>
                <label>
                  <span>Cover URL</span>
                  <input
                    className={fieldClass(bookErrors, 'coverUrl')}
                    aria-invalid={hasFieldError(bookErrors, 'coverUrl')}
                    value={bookForm.coverUrl}
                    onChange={(event) => setBookFormField('coverUrl', event.target.value)}
                  />
                  <FieldMessages errors={bookErrors} field="coverUrl" />
                </label>
              </div>

              <label htmlFor="book-creator-credit">Creator Credit</label>
              <input
                id="book-creator-credit"
                className={fieldClass(bookErrors, 'creatorCredit')}
                aria-invalid={hasFieldError(bookErrors, 'creatorCredit')}
                value={bookForm.creatorCredit}
                onChange={(event) => setBookFormField('creatorCredit', event.target.value)}
              />
              <FieldMessages errors={bookErrors} field="creatorCredit" />

              <label htmlFor="book-description">Description</label>
              <textarea
                id="book-description"
                className={fieldClass(bookErrors, 'description')}
                aria-invalid={hasFieldError(bookErrors, 'description')}
                value={bookForm.description}
                onChange={(event) => setBookFormField('description', event.target.value)}
                rows={4}
              />
              <FieldMessages errors={bookErrors} field="description" />

              <label className="checkbox">
                <input type="checkbox" checked={bookForm.publishOnSite} onChange={(event) => setBookFormField('publishOnSite', event.target.checked)} />
                <span>Publish on site</span>
              </label>

              {bookMutationError && <ProblemMessage error={bookMutationError} />}
              <div className="actions">
                <button type="submit" disabled={createBook.isPending || updateBook.isPending}>
                  {editingBookId ? 'Save' : 'Create'}
                </button>
                {editingBookId && (
                  <button type="button" className="secondary" onClick={cancelBookEdit}>
                    Cancel
                  </button>
                )}
              </div>
            </form>
          </aside>

          <section className="content-area">
            <div className="section-heading">
              <div>
                <h2>Books</h2>
                <p>{booksQuery.data?.total ?? 0} active records</p>
              </div>
              <div className="filters">
                <label className="search">
                  <span>Search</span>
                  <input value={bookSearch} onChange={(event) => setBookSearch(event.target.value)} />
                </label>
                <label className="search">
                  <span>Author</span>
                  <select value={bookAuthorFilter} onChange={(event) => setBookAuthorFilter(event.target.value)}>
                    <option value="">All</option>
                    {authors.map((author) => (
                      <option key={author.id} value={author.id}>
                        {author.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="search">
                  <span>Genre</span>
                  <select value={bookGenreFilter} onChange={(event) => setBookGenreFilter(event.target.value)}>
                    <option value="">All</option>
                    {genres.map((genre) => (
                      <option key={genre.id} value={genre.id}>
                        {genre.name}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>

            {booksQuery.isPending && <p className="state">Loading books...</p>}
            {booksQuery.isError && <ProblemMessage error={booksQuery.error} />}
            {booksQuery.data?.items.length === 0 && <p className="state">No books found.</p>}

            <div className="table" role="table" aria-label="Books">
              {booksQuery.data?.items.map((book) => (
                <div className="table-row book-row" role="row" key={book.id}>
                  <div className="book-summary">
                    <BookCover title={book.title} coverUrl={book.coverUrl} />
                    <div>
                      <strong>{book.title}</strong>
                      <p>{book.authorName} · {book.genreName}</p>
                      <p>{[book.publisher, book.publishedOn, book.isbn13].filter(Boolean).join(' · ')}</p>
                    </div>
                  </div>
                  <div className="row-actions">
                    <button type="button" className="secondary" onClick={() => editBook(book)}>
                      Edit
                    </button>
                    <button type="button" className="danger" disabled={deleteBook.isPending} onClick={() => deleteBook.mutate(book.id)}>
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </section>
      ) : (
        <section className="workspace" aria-label={`${tab} management`}>
          <aside className="panel">
            <h2>{editingReference ? `Edit ${singular(tab)}` : `New ${singular(tab)}`}</h2>
            {editingReference ? (
              <form onSubmit={submitReferenceEdit} className="form-stack">
                <label htmlFor="reference-edit-name">Name</label>
                <input
                  id="reference-edit-name"
                  className={fieldClass(referenceErrors, 'name')}
                  aria-invalid={hasFieldError(referenceErrors, 'name')}
                  value={editingReferenceName}
                  onChange={(event) => setEditingReferenceName(event.target.value)}
                  autoFocus
                />
                <FieldMessages errors={referenceErrors} field="name" />
                {referenceMutationError && <ProblemMessage error={referenceMutationError} />}
                <div className="actions">
                  <button type="submit" disabled={updateReference.isPending}>
                    Save
                  </button>
                  <button type="button" className="secondary" onClick={() => setEditingReference(null)}>
                    Cancel
                  </button>
                </div>
              </form>
            ) : (
              <form onSubmit={submitReference} className="form-stack">
                <label htmlFor="reference-name">Name</label>
                <input
                  id="reference-name"
                  className={fieldClass(referenceErrors, 'name')}
                  aria-invalid={hasFieldError(referenceErrors, 'name')}
                  value={referenceName}
                  onChange={(event) => setReferenceName(event.target.value)}
                />
                <FieldMessages errors={referenceErrors} field="name" />
                {referenceMutationError && <ProblemMessage error={referenceMutationError} />}
                <button type="submit" disabled={createReference.isPending}>
                  Create
                </button>
              </form>
            )}
          </aside>

          <section className="content-area">
            <div className="section-heading">
              <div>
                <h2>{titleCase(tab)}</h2>
                <p>{referenceQuery.data?.total ?? 0} active records</p>
              </div>
              <label className="search">
                <span>Search</span>
                <input value={referenceSearch} onChange={(event) => setReferenceSearch(event.target.value)} />
              </label>
            </div>

            {referenceQuery.isPending && <p className="state">Loading {tab}...</p>}
            {referenceQuery.isError && <ProblemMessage error={referenceQuery.error} />}
            {referenceQuery.data?.items.length === 0 && <p className="state">No {tab} found.</p>}

            <div className="table" role="table" aria-label={titleCase(tab)}>
              {referenceQuery.data?.items.map((record) => (
                <div className="table-row" role="row" key={record.id}>
                  <div>
                    <strong>{record.name}</strong>
                    {record.isSystem && <span className="pill">System</span>}
                  </div>
                  <div className="row-actions">
                    <button type="button" className="secondary" disabled={record.isSystem} onClick={() => startReferenceEdit(record)}>
                      Edit
                    </button>
                    <button
                      type="button"
                      className="danger"
                      disabled={record.isSystem || deleteReference.isPending}
                      onClick={() => deleteReference.mutate({ type: referenceType, id: record.id })}
                    >
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </section>
      )}
    </main>
  )

  function setBookFormField<Key extends keyof BookForm>(key: Key, value: BookForm[Key]) {
    setBookForm((current) => ({ ...current, [key]: value }))
  }
}

async function fetchReferences(type: Tab, search: string, pageSize: number) {
  const response = await api.get<ReferenceListResponse>(`/api/v1/${type}`, {
    params: { search: search || undefined, pageSize },
  })
  return response.data
}

function toBookPayload(form: BookForm) {
  return {
    title: form.title,
    authorId: form.authorId || null,
    genreId: form.genreId || null,
    creatorCredit: form.creatorCredit || null,
    isbn13: form.isbn13 || null,
    isbn10: form.isbn10 || null,
    description: form.description || null,
    publisher: form.publisher || null,
    publishedOn: form.publishedOn || null,
    pageCount: form.pageCount ? Number(form.pageCount) : null,
    copyCount: form.copyCount ? Number(form.copyCount) : null,
    coverUrl: form.coverUrl || null,
    collectionName: form.collectionName || null,
    sourceAddedOn: form.sourceAddedOn || null,
    publishOnSite: form.publishOnSite,
  }
}

function BookCover({ title, coverUrl }: { title: string; coverUrl: string | null }) {
  const [failed, setFailed] = useState(false)

  if (!coverUrl || failed) {
    return (
      <div className="book-cover-placeholder" aria-hidden="true">
        <span>{title.trim().charAt(0).toUpperCase() || 'B'}</span>
      </div>
    )
  }

  return (
    <img
      className="book-cover"
      src={coverUrl}
      alt={`Cover of ${title}`}
      loading="lazy"
      onError={() => setFailed(true)}
    />
  )
}

function ProblemMessage({ error }: { error: unknown }) {
  const problem = getProblem(error)
  const fieldMessages = Object.entries(problem.errors)

  return (
    <div className="problem" role="alert">
      <strong>{problem.message}</strong>
      {fieldMessages.length > 0 && (
        <ul>
          {fieldMessages.flatMap(([field, messages]) =>
            messages.map((message) => (
              <li key={`${field}-${message}`}>
                {fieldLabel(field)}: {message}
              </li>
            )),
          )}
        </ul>
      )}
    </div>
  )
}

function AuthShell({ children, healthLabel }: { children: React.ReactNode; healthLabel: string }) {
  return (
    <main className="app-shell auth-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Books Library</p>
          <h1>Catalog Administration</h1>
        </div>
        <div className={`health health-${healthLabel}`}>
          <span aria-hidden="true" />
          API {healthLabel}
        </div>
      </header>
      {children}
    </main>
  )
}

function readSession(): Session | null {
  try {
    const value = localStorage.getItem('books-lib-session')
    if (!value) {
      return null
    }

    const parsed = JSON.parse(value) as Partial<Session>
    if (!parsed.accessToken || !parsed.email || typeof parsed.passwordChangeRequired !== 'boolean') {
      return null
    }

    return {
      accessToken: parsed.accessToken,
      email: parsed.email,
      passwordChangeRequired: parsed.passwordChangeRequired,
    }
  } catch {
    return null
  }
}

function FieldMessages({ errors, field }: { errors: ValidationErrors; field: string }) {
  const messages = errors[field] ?? []
  if (messages.length === 0) {
    return null
  }

  return (
    <ul className="field-errors">
      {messages.map((message) => (
        <li key={message}>{message}</li>
      ))}
    </ul>
  )
}

function getValidationErrors(error: unknown): ValidationErrors {
  return getProblem(error).errors
}

function getProblem(error: unknown): { message: string; errors: ValidationErrors } {
  if (!axios.isAxiosError(error)) {
    return { message: 'Request failed.', errors: {} }
  }

  const data = error.response?.data
  const errors = isValidationErrors(data?.errors) ? data.errors : {}
  const detail = typeof data?.detail === 'string' ? data.detail : null
  const title = typeof data?.title === 'string' ? data.title : null

  return {
    message: detail ?? title ?? 'Request failed.',
    errors,
  }
}

function isValidationErrors(value: unknown): value is ValidationErrors {
  if (!value || typeof value !== 'object') {
    return false
  }

  return Object.values(value).every(
    (messages) => Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  )
}

function hasFieldError(errors: ValidationErrors, field: string) {
  return (errors[field]?.length ?? 0) > 0
}

function fieldClass(errors: ValidationErrors, field: string) {
  return hasFieldError(errors, field) ? 'invalid-field' : undefined
}

function fieldLabel(field: string) {
  const labels: Record<string, string> = {
    authorId: 'Author',
    collectionName: 'Collection',
    copyCount: 'Copies',
    coverUrl: 'Cover URL',
    creatorCredit: 'Creator Credit',
    currentPassword: 'Current Password',
    description: 'Description',
    email: 'Email',
    genreId: 'Genre',
    isbn10: 'ISBN-10',
    isbn13: 'ISBN-13',
    name: 'Name',
    newPassword: 'New Password',
    pageCount: 'Page Count',
    password: 'Password',
    publisher: 'Publisher',
    title: 'Title',
  }

  return labels[field] ?? field
}

function singular(value: Tab) {
  return value === 'authors' ? 'Author' : value === 'genres' ? 'Genre' : 'Book'
}

function titleCase(value: string) {
  return value.slice(0, 1).toUpperCase() + value.slice(1)
}

export default App
