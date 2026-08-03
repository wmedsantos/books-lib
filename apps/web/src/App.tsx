import { FormEvent, useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import './App.css'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080',
})

type Tab = 'books' | 'authors' | 'genres'
type Language = 'en' | 'pt' | 'es'

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

const bookPageSize = 10

const languages: Array<{ code: Language; label: string }> = [
  { code: 'en', label: 'English' },
  { code: 'pt', label: 'Português' },
  { code: 'es', label: 'Español' },
]

const translations = {
  en: {
    activeRecords: 'active records',
    all: 'All',
    api: 'API',
    author: 'Author',
    authors: 'Authors',
    books: 'Books',
    cancel: 'Cancel',
    catalogAdministration: 'Catalog Administration',
    catalogSections: 'Catalog sections',
    catalogSignIn: 'Catalog Sign In',
    changePassword: 'Change Password',
    collection: 'Collection',
    copies: 'Copies',
    coverOf: 'Cover of',
    coverUrl: 'Cover URL',
    create: 'Create',
    creatorCredit: 'Creator Credit',
    currentPassword: 'Current Password',
    delete: 'Delete',
    description: 'Description',
    edit: 'Edit',
    editAuthor: 'Edit Author',
    editBook: 'Edit Book',
    editGenre: 'Edit Genre',
    email: 'Email',
    genre: 'Genre',
    genres: 'Genres',
    language: 'Language',
    loadingBooks: 'Loading books...',
    loadingRecords: 'Loading records...',
    name: 'Name',
    newAuthor: 'New Author',
    newBook: 'New Book',
    newGenre: 'New Genre',
    newPassword: 'New Password',
    next: 'Next',
    noBooksFound: 'No books found.',
    noRecordsFound: 'No records found.',
    page: 'Page',
    pageCount: 'Page Count',
    password: 'Password',
    previous: 'Previous',
    publishOnSite: 'Publish on site',
    published: 'Published',
    publisher: 'Publisher',
    requestFailed: 'Request failed.',
    save: 'Save',
    savePassword: 'Save Password',
    search: 'Search',
    selectAuthor: 'Select author',
    selectGenre: 'Select genre',
    showing: 'Showing',
    signIn: 'Sign In',
    signOut: 'Sign Out',
    system: 'System',
    title: 'Title',
    of: 'of',
    statusChecking: 'checking',
    statusDegraded: 'degraded',
    statusOffline: 'offline',
    statusReady: 'ready',
  },
  pt: {
    activeRecords: 'registros ativos',
    all: 'Todos',
    api: 'API',
    author: 'Autor',
    authors: 'Autores',
    books: 'Livros',
    cancel: 'Cancelar',
    catalogAdministration: 'Administração do Catálogo',
    catalogSections: 'Seções do catálogo',
    catalogSignIn: 'Entrar no Catálogo',
    changePassword: 'Alterar Senha',
    collection: 'Coleção',
    copies: 'Cópias',
    coverOf: 'Capa de',
    coverUrl: 'URL da capa',
    create: 'Criar',
    creatorCredit: 'Crédito de criação',
    currentPassword: 'Senha atual',
    delete: 'Excluir',
    description: 'Descrição',
    edit: 'Editar',
    editAuthor: 'Editar Autor',
    editBook: 'Editar Livro',
    editGenre: 'Editar Gênero',
    email: 'Email',
    genre: 'Gênero',
    genres: 'Gêneros',
    language: 'Idioma',
    loadingBooks: 'Carregando livros...',
    loadingRecords: 'Carregando registros...',
    name: 'Nome',
    newAuthor: 'Novo Autor',
    newBook: 'Novo Livro',
    newGenre: 'Novo Gênero',
    newPassword: 'Nova senha',
    next: 'Próxima',
    noBooksFound: 'Nenhum livro encontrado.',
    noRecordsFound: 'Nenhum registro encontrado.',
    page: 'Página',
    pageCount: 'Número de páginas',
    password: 'Senha',
    previous: 'Anterior',
    publishOnSite: 'Publicar no site',
    published: 'Publicado',
    publisher: 'Editora',
    requestFailed: 'A requisição falhou.',
    save: 'Salvar',
    savePassword: 'Salvar senha',
    search: 'Buscar',
    selectAuthor: 'Selecione o autor',
    selectGenre: 'Selecione o gênero',
    showing: 'Mostrando',
    signIn: 'Entrar',
    signOut: 'Sair',
    system: 'Sistema',
    title: 'Título',
    of: 'de',
    statusChecking: 'verificando',
    statusDegraded: 'degradada',
    statusOffline: 'offline',
    statusReady: 'pronta',
  },
  es: {
    activeRecords: 'registros activos',
    all: 'Todos',
    api: 'API',
    author: 'Autor',
    authors: 'Autores',
    books: 'Libros',
    cancel: 'Cancelar',
    catalogAdministration: 'Administración del Catálogo',
    catalogSections: 'Secciones del catálogo',
    catalogSignIn: 'Acceso al Catálogo',
    changePassword: 'Cambiar Contraseña',
    collection: 'Colección',
    copies: 'Copias',
    coverOf: 'Portada de',
    coverUrl: 'URL de portada',
    create: 'Crear',
    creatorCredit: 'Crédito de creación',
    currentPassword: 'Contraseña actual',
    delete: 'Eliminar',
    description: 'Descripción',
    edit: 'Editar',
    editAuthor: 'Editar Autor',
    editBook: 'Editar Libro',
    editGenre: 'Editar Género',
    email: 'Email',
    genre: 'Género',
    genres: 'Géneros',
    language: 'Idioma',
    loadingBooks: 'Cargando libros...',
    loadingRecords: 'Cargando registros...',
    name: 'Nombre',
    newAuthor: 'Nuevo Autor',
    newBook: 'Nuevo Libro',
    newGenre: 'Nuevo Género',
    newPassword: 'Nueva contraseña',
    next: 'Siguiente',
    noBooksFound: 'No se encontraron libros.',
    noRecordsFound: 'No se encontraron registros.',
    page: 'Página',
    pageCount: 'Número de páginas',
    password: 'Contraseña',
    previous: 'Anterior',
    publishOnSite: 'Publicar en el sitio',
    published: 'Publicado',
    publisher: 'Editorial',
    requestFailed: 'La solicitud falló.',
    save: 'Guardar',
    savePassword: 'Guardar contraseña',
    search: 'Buscar',
    selectAuthor: 'Seleccione autor',
    selectGenre: 'Seleccione género',
    showing: 'Mostrando',
    signIn: 'Entrar',
    signOut: 'Salir',
    system: 'Sistema',
    title: 'Título',
    of: 'de',
    statusChecking: 'verificando',
    statusDegraded: 'degradada',
    statusOffline: 'offline',
    statusReady: 'lista',
  },
} as const

type TranslationSet = Record<keyof typeof translations.en, string>

function App() {
  const queryClient = useQueryClient()
  const [language, setLanguage] = useState<Language>(() => readLanguageCookie())
  const [session, setSession] = useState<Session | null>(() => readSession())
  const [loginEmail, setLoginEmail] = useState('admin@bookslib.local')
  const [loginPassword, setLoginPassword] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [tab, setTab] = useState<Tab>('books')
  const [bookSearch, setBookSearch] = useState('')
  const [bookAuthorFilter, setBookAuthorFilter] = useState('')
  const [bookGenreFilter, setBookGenreFilter] = useState('')
  const [bookPage, setBookPage] = useState(1)
  const [referenceSearch, setReferenceSearch] = useState('')
  const [bookForm, setBookForm] = useState<BookForm>(emptyBookForm)
  const [editingBookId, setEditingBookId] = useState<string | null>(null)
  const [referenceName, setReferenceName] = useState('')
  const [editingReference, setEditingReference] = useState<ReferenceRecord | null>(null)
  const [editingReferenceName, setEditingReferenceName] = useState('')
  const t: TranslationSet = translations[language]

  useEffect(() => {
    document.documentElement.lang = language
    document.cookie = `books-lib-language=${language}; Max-Age=31536000; Path=/; SameSite=Lax`
  }, [language])

  useEffect(() => {
    if (session) {
      api.defaults.headers.common.Authorization = `Bearer ${session.accessToken}`
      localStorage.setItem('books-lib-session', JSON.stringify(session))
    } else {
      delete api.defaults.headers.common.Authorization
      localStorage.removeItem('books-lib-session')
    }
  }, [session])

  useEffect(() => {
    setBookPage(1)
  }, [bookSearch, bookAuthorFilter, bookGenreFilter])

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
    queryKey: ['books', bookSearch, bookAuthorFilter, bookGenreFilter, bookPage],
    queryFn: async () => {
      const response = await api.get<BookListResponse>('/api/v1/books', {
        params: {
          search: bookSearch || undefined,
          authorId: bookAuthorFilter || undefined,
          genreId: bookGenreFilter || undefined,
          page: bookPage,
          pageSize: bookPageSize,
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
      setBookPage(1)
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
      }, {
        headers: session?.accessToken
          ? { Authorization: `Bearer ${session.accessToken}` }
          : undefined,
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
  const bookTotal = booksQuery.data?.total ?? 0
  const bookTotalPages = Math.max(1, Math.ceil(bookTotal / bookPageSize))
  const bookPageStart = bookTotal === 0 ? 0 : (bookPage - 1) * bookPageSize + 1
  const bookPageEnd = Math.min(bookPage * bookPageSize, bookTotal)

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
      <AuthShell healthLabel={healthLabel} language={language} onLanguageChange={setLanguage} t={t}>
        <form
          className="auth-card form-stack"
          onSubmit={(event) => {
            event.preventDefault()
            login.mutate()
          }}
        >
          <h1>{t.catalogSignIn}</h1>
          <label htmlFor="login-email">{t.email}</label>
          <input
            id="login-email"
            className={fieldClass(loginErrors, 'email')}
            aria-invalid={hasFieldError(loginErrors, 'email')}
            value={loginEmail}
            onChange={(event) => setLoginEmail(event.target.value)}
          />
          <FieldMessages errors={loginErrors} field="email" />

          <label htmlFor="login-password">{t.password}</label>
          <input
            id="login-password"
            className={fieldClass(loginErrors, 'password')}
            aria-invalid={hasFieldError(loginErrors, 'password')}
            type="password"
            value={loginPassword}
            onChange={(event) => setLoginPassword(event.target.value)}
          />
          <FieldMessages errors={loginErrors} field="password" />

          {login.isError && <ProblemMessage error={login.error} t={t} />}
          <button type="submit" disabled={login.isPending}>
            {t.signIn}
          </button>
        </form>
      </AuthShell>
    )
  }

  if (session.passwordChangeRequired) {
    return (
      <AuthShell healthLabel={healthLabel} language={language} onLanguageChange={setLanguage} t={t}>
        <form
          className="auth-card form-stack"
          onSubmit={(event) => {
            event.preventDefault()
            changePassword.mutate()
          }}
        >
          <h1>{t.changePassword}</h1>
          <p className="muted">{session.email}</p>
          <label htmlFor="current-password">{t.currentPassword}</label>
          <input
            id="current-password"
            className={fieldClass(changePasswordErrors, 'currentPassword')}
            aria-invalid={hasFieldError(changePasswordErrors, 'currentPassword')}
            type="password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
          />
          <FieldMessages errors={changePasswordErrors} field="currentPassword" />

          <label htmlFor="new-password">{t.newPassword}</label>
          <input
            id="new-password"
            className={fieldClass(changePasswordErrors, 'newPassword')}
            aria-invalid={hasFieldError(changePasswordErrors, 'newPassword')}
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
          />
          <FieldMessages errors={changePasswordErrors} field="newPassword" />

          {changePassword.isError && <ProblemMessage error={changePassword.error} t={t} />}
          <div className="actions">
            <button type="submit" disabled={changePassword.isPending}>
              {t.savePassword}
            </button>
            <button type="button" className="secondary" onClick={() => setSession(null)}>
              {t.signOut}
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
          <h1>{t.catalogAdministration}</h1>
        </div>
        <TopbarActions
          healthLabel={healthLabel}
          language={language}
          onLanguageChange={setLanguage}
          onSignOut={() => setSession(null)}
          t={t}
        />
      </header>

      <nav className="tabs" aria-label={t.catalogSections}>
        <button type="button" className={tab === 'books' ? 'active' : ''} onClick={() => setTab('books')}>
          {t.books}
        </button>
        <button type="button" className={tab === 'authors' ? 'active' : ''} onClick={() => setTab('authors')}>
          {t.authors}
        </button>
        <button type="button" className={tab === 'genres' ? 'active' : ''} onClick={() => setTab('genres')}>
          {t.genres}
        </button>
      </nav>

      {tab === 'books' ? (
        <section className="workspace book-workspace" aria-label="Book management">
          <aside className="panel">
            <h2>{editingBookId ? t.editBook : t.newBook}</h2>
            <form onSubmit={submitBook} className="form-stack">
              <label htmlFor="book-title">{t.title}</label>
              <input
                id="book-title"
                className={fieldClass(bookErrors, 'title')}
                aria-invalid={hasFieldError(bookErrors, 'title')}
                value={bookForm.title}
                onChange={(event) => setBookFormField('title', event.target.value)}
                maxLength={240}
              />
              <FieldMessages errors={bookErrors} field="title" />

              <label htmlFor="book-author">{t.author}</label>
              <select
                id="book-author"
                className={fieldClass(bookErrors, 'authorId')}
                aria-invalid={hasFieldError(bookErrors, 'authorId')}
                value={bookForm.authorId}
                onChange={(event) => setBookFormField('authorId', event.target.value)}
              >
                <option value="">{t.selectAuthor}</option>
                {authors.map((author) => (
                  <option key={author.id} value={author.id}>
                    {author.name}
                  </option>
                ))}
              </select>
              <FieldMessages errors={bookErrors} field="authorId" />

              <label htmlFor="book-genre">{t.genre}</label>
              <select
                id="book-genre"
                className={fieldClass(bookErrors, 'genreId')}
                aria-invalid={hasFieldError(bookErrors, 'genreId')}
                value={bookForm.genreId}
                onChange={(event) => setBookFormField('genreId', event.target.value)}
              >
                <option value="">{t.selectGenre}</option>
                {genres.map((genre) => (
                  <option key={genre.id} value={genre.id}>
                    {genre.name}
                  </option>
                ))}
              </select>
              <FieldMessages errors={bookErrors} field="genreId" />

              <label htmlFor="book-copies">{t.copies}</label>
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
                  <span>{t.publisher}</span>
                  <input
                    className={fieldClass(bookErrors, 'publisher')}
                    aria-invalid={hasFieldError(bookErrors, 'publisher')}
                    value={bookForm.publisher}
                    onChange={(event) => setBookFormField('publisher', event.target.value)}
                  />
                  <FieldMessages errors={bookErrors} field="publisher" />
                </label>
                <label>
                  <span>{t.published}</span>
                  <input type="date" value={bookForm.publishedOn} onChange={(event) => setBookFormField('publishedOn', event.target.value)} />
                </label>
              </div>

              <div className="form-grid">
                <label>
                  <span>{t.pageCount}</span>
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
                  <span>{t.coverUrl}</span>
                  <input
                    className={fieldClass(bookErrors, 'coverUrl')}
                    aria-invalid={hasFieldError(bookErrors, 'coverUrl')}
                    value={bookForm.coverUrl}
                    onChange={(event) => setBookFormField('coverUrl', event.target.value)}
                  />
                  <FieldMessages errors={bookErrors} field="coverUrl" />
                </label>
              </div>

              <label htmlFor="book-creator-credit">{t.creatorCredit}</label>
              <input
                id="book-creator-credit"
                className={fieldClass(bookErrors, 'creatorCredit')}
                aria-invalid={hasFieldError(bookErrors, 'creatorCredit')}
                value={bookForm.creatorCredit}
                onChange={(event) => setBookFormField('creatorCredit', event.target.value)}
              />
              <FieldMessages errors={bookErrors} field="creatorCredit" />

              <label htmlFor="book-description">{t.description}</label>
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
                <span>{t.publishOnSite}</span>
              </label>

              {bookMutationError && <ProblemMessage error={bookMutationError} t={t} />}
              <div className="actions">
                <button type="submit" disabled={createBook.isPending || updateBook.isPending}>
                  {editingBookId ? t.save : t.create}
                </button>
                {editingBookId && (
                  <button type="button" className="secondary" onClick={cancelBookEdit}>
                    {t.cancel}
                  </button>
                )}
              </div>
            </form>
          </aside>

          <section className="content-area">
            <div className="section-heading">
              <div>
                <h2>{t.books}</h2>
                <p>{booksQuery.data?.total ?? 0} {t.activeRecords}</p>
              </div>
              <div className="filters">
                <label className="search">
                  <span>{t.search}</span>
                  <input value={bookSearch} onChange={(event) => setBookSearch(event.target.value)} />
                </label>
                <label className="search">
                  <span>{t.author}</span>
                  <select value={bookAuthorFilter} onChange={(event) => setBookAuthorFilter(event.target.value)}>
                    <option value="">{t.all}</option>
                    {authors.map((author) => (
                      <option key={author.id} value={author.id}>
                        {author.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="search">
                  <span>{t.genre}</span>
                  <select value={bookGenreFilter} onChange={(event) => setBookGenreFilter(event.target.value)}>
                    <option value="">{t.all}</option>
                    {genres.map((genre) => (
                      <option key={genre.id} value={genre.id}>
                        {genre.name}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>

            {booksQuery.isPending && <p className="state">{t.loadingBooks}</p>}
            {booksQuery.isError && <ProblemMessage error={booksQuery.error} t={t} />}
            {booksQuery.data?.items.length === 0 && <p className="state">{t.noBooksFound}</p>}

            <div className="table" role="table" aria-label="Books">
              {booksQuery.data?.items.map((book) => (
                <div className="table-row book-row" role="row" key={book.id}>
                  <div className="book-summary">
                    <BookCover title={book.title} coverUrl={book.coverUrl} coverOfLabel={t.coverOf} />
                    <div>
                      <strong>{book.title}</strong>
                      <p>{book.authorName} · {book.genreName}</p>
                      <p>{[book.publisher, book.publishedOn, book.isbn13].filter(Boolean).join(' · ')}</p>
                    </div>
                  </div>
                  <div className="row-actions">
                    <button type="button" className="secondary" onClick={() => editBook(book)}>
                      {t.edit}
                    </button>
                    <button type="button" className="danger" disabled={deleteBook.isPending} onClick={() => deleteBook.mutate(book.id)}>
                      {t.delete}
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {bookTotal > 0 && (
              <nav className="pagination" aria-label="Book list pagination">
                <p>
                  {t.showing} {bookPageStart}-{bookPageEnd} {t.of} {bookTotal}
                </p>
                <div className="pagination-controls">
                  <button
                    type="button"
                    className="secondary"
                    disabled={bookPage <= 1 || booksQuery.isFetching}
                    onClick={() => setBookPage((current) => Math.max(1, current - 1))}
                  >
                    {t.previous}
                  </button>
                  <span>
                    {t.page} {bookPage} {t.of} {bookTotalPages}
                  </span>
                  <button
                    type="button"
                    className="secondary"
                    disabled={bookPage >= bookTotalPages || booksQuery.isFetching}
                    onClick={() => setBookPage((current) => Math.min(bookTotalPages, current + 1))}
                  >
                    {t.next}
                  </button>
                </div>
              </nav>
            )}
          </section>
        </section>
      ) : (
        <section className="workspace" aria-label={`${tab} management`}>
          <aside className="panel">
            <h2>{editingReference ? editReferenceTitle(tab, t) : newReferenceTitle(tab, t)}</h2>
            {editingReference ? (
              <form onSubmit={submitReferenceEdit} className="form-stack">
                <label htmlFor="reference-edit-name">{t.name}</label>
                <input
                  id="reference-edit-name"
                  className={fieldClass(referenceErrors, 'name')}
                  aria-invalid={hasFieldError(referenceErrors, 'name')}
                  value={editingReferenceName}
                  onChange={(event) => setEditingReferenceName(event.target.value)}
                  autoFocus
                />
                <FieldMessages errors={referenceErrors} field="name" />
                {referenceMutationError && <ProblemMessage error={referenceMutationError} t={t} />}
                <div className="actions">
                  <button type="submit" disabled={updateReference.isPending}>
                    {t.save}
                  </button>
                  <button type="button" className="secondary" onClick={() => setEditingReference(null)}>
                    {t.cancel}
                  </button>
                </div>
              </form>
            ) : (
              <form onSubmit={submitReference} className="form-stack">
                <label htmlFor="reference-name">{t.name}</label>
                <input
                  id="reference-name"
                  className={fieldClass(referenceErrors, 'name')}
                  aria-invalid={hasFieldError(referenceErrors, 'name')}
                  value={referenceName}
                  onChange={(event) => setReferenceName(event.target.value)}
                />
                <FieldMessages errors={referenceErrors} field="name" />
                {referenceMutationError && <ProblemMessage error={referenceMutationError} t={t} />}
                <button type="submit" disabled={createReference.isPending}>
                  {t.create}
                </button>
              </form>
            )}
          </aside>

          <section className="content-area">
            <div className="section-heading">
              <div>
                <h2>{tabLabel(tab, t)}</h2>
                <p>{referenceQuery.data?.total ?? 0} {t.activeRecords}</p>
              </div>
              <label className="search">
                <span>{t.search}</span>
                <input value={referenceSearch} onChange={(event) => setReferenceSearch(event.target.value)} />
              </label>
            </div>

            {referenceQuery.isPending && <p className="state">{t.loadingRecords}</p>}
            {referenceQuery.isError && <ProblemMessage error={referenceQuery.error} t={t} />}
            {referenceQuery.data?.items.length === 0 && <p className="state">{t.noRecordsFound}</p>}

            <div className="table" role="table" aria-label={tabLabel(tab, t)}>
              {referenceQuery.data?.items.map((record) => (
                <div className="table-row" role="row" key={record.id}>
                  <div>
                    <strong>{record.name}</strong>
                    {record.isSystem && <span className="pill">{t.system}</span>}
                  </div>
                  <div className="row-actions">
                    <button type="button" className="secondary" disabled={record.isSystem} onClick={() => startReferenceEdit(record)}>
                      {t.edit}
                    </button>
                    <button
                      type="button"
                      className="danger"
                      disabled={record.isSystem || deleteReference.isPending}
                      onClick={() => deleteReference.mutate({ type: referenceType, id: record.id })}
                    >
                      {t.delete}
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

function BookCover({ title, coverUrl, coverOfLabel }: { title: string; coverUrl: string | null; coverOfLabel: string }) {
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
      alt={`${coverOfLabel} ${title}`}
      loading="lazy"
      onError={() => setFailed(true)}
    />
  )
}

function ProblemMessage({ error, t }: { error: unknown; t: TranslationSet }) {
  const problem = getProblem(error, t)
  const fieldMessages = Object.entries(problem.errors)

  return (
    <div className="problem" role="alert">
      <strong>{problem.message}</strong>
      {fieldMessages.length > 0 && (
        <ul>
          {fieldMessages.flatMap(([field, messages]) =>
            messages.map((message) => (
              <li key={`${field}-${message}`}>
                {fieldLabel(field, t)}: {message}
              </li>
            )),
          )}
        </ul>
      )}
    </div>
  )
}

function AuthShell({
  children,
  healthLabel,
  language,
  onLanguageChange,
  t,
}: {
  children: React.ReactNode
  healthLabel: string
  language: Language
  onLanguageChange: (language: Language) => void
  t: TranslationSet
}) {
  return (
    <main className="app-shell auth-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Books Library</p>
          <h1>{t.catalogAdministration}</h1>
        </div>
        <TopbarActions
          healthLabel={healthLabel}
          language={language}
          onLanguageChange={onLanguageChange}
          t={t}
        />
      </header>
      {children}
    </main>
  )
}

function TopbarActions({
  healthLabel,
  language,
  onLanguageChange,
  onSignOut,
  t,
}: {
  healthLabel: string
  language: Language
  onLanguageChange: (language: Language) => void
  onSignOut?: () => void
  t: TranslationSet
}) {
  return (
    <div className="topbar-actions" aria-label="Header actions">
      <div className={`health health-${healthLabel}`}>
        <span aria-hidden="true" />
        {t.api} {healthText(healthLabel, t)}
      </div>
      <LanguageSelector language={language} onLanguageChange={onLanguageChange} t={t} />
      {onSignOut && (
        <button type="button" className="icon-button" onClick={onSignOut}>
          <LogoutIcon />
          <span>{t.signOut}</span>
        </button>
      )}
    </div>
  )
}

function LanguageSelector({
  language,
  onLanguageChange,
  t,
}: {
  language: Language
  onLanguageChange: (language: Language) => void
  t: TranslationSet
}) {
  return (
    <label className="language-select">
      <span className="language-label">
        <LanguageIcon />
        {t.language}
      </span>
      <span className="language-control">
        <select value={language} onChange={(event) => onLanguageChange(event.target.value as Language)}>
          {languages.map((option) => (
            <option key={option.code} value={option.code}>
              {option.label}
            </option>
          ))}
        </select>
      </span>
    </label>
  )
}

function LanguageIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="M5 5h8" />
      <path d="M9 3v2" />
      <path d="M11 5c-.7 2.8-2.7 5.2-6 7" />
      <path d="M7.5 8.5c1.1 1.6 2.6 2.8 4.5 3.5" />
      <path d="M14 19l4-10 4 10" />
      <path d="M15.5 15h5" />
    </svg>
  )
}

function LogoutIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="M10 17l5-5-5-5" />
      <path d="M15 12H3" />
      <path d="M14 4h4a3 3 0 0 1 3 3v10a3 3 0 0 1-3 3h-4" />
    </svg>
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

function readLanguageCookie(): Language {
  const cookie = document.cookie
    .split('; ')
    .find((value) => value.startsWith('books-lib-language='))
    ?.split('=')[1]

  return isLanguage(cookie) ? cookie : 'en'
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
  return getProblem(error, translations.en).errors
}

function getProblem(error: unknown, t: TranslationSet): { message: string; errors: ValidationErrors } {
  if (!axios.isAxiosError(error)) {
    return { message: t.requestFailed, errors: {} }
  }

  const data = error.response?.data
  const errors = isValidationErrors(data?.errors) ? data.errors : {}
  const detail = typeof data?.detail === 'string' ? data.detail : null
  const title = typeof data?.title === 'string' ? data.title : null

  return {
    message: detail ?? title ?? t.requestFailed,
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

function fieldLabel(field: string, t: TranslationSet) {
  const labels: Record<string, string> = {
    authorId: t.author,
    collectionName: t.collection,
    copyCount: t.copies,
    coverUrl: t.coverUrl,
    creatorCredit: t.creatorCredit,
    currentPassword: t.currentPassword,
    description: t.description,
    email: t.email,
    genreId: t.genre,
    isbn10: 'ISBN-10',
    isbn13: 'ISBN-13',
    name: t.name,
    newPassword: t.newPassword,
    pageCount: t.pageCount,
    password: t.password,
    publisher: t.publisher,
    title: t.title,
  }

  return labels[field] ?? field
}

function isLanguage(value: string | undefined): value is Language {
  return value === 'en' || value === 'pt' || value === 'es'
}

function tabLabel(value: Tab, t: TranslationSet) {
  if (value === 'authors') return t.authors
  if (value === 'genres') return t.genres
  return t.books
}

function newReferenceTitle(value: Tab, t: TranslationSet) {
  return value === 'authors' ? t.newAuthor : t.newGenre
}

function editReferenceTitle(value: Tab, t: TranslationSet) {
  return value === 'authors' ? t.editAuthor : t.editGenre
}

function healthText(value: string, t: TranslationSet) {
  if (value === 'ready') return t.statusReady
  if (value === 'offline') return t.statusOffline
  if (value === 'degraded') return t.statusDegraded
  return t.statusChecking
}

export default App
