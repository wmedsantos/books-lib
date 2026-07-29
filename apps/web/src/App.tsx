import { FormEvent, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import './App.css'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080',
})

type Genre = {
  id: string
  name: string
  systemCode: string | null
  isSystem: boolean
}

type GenreListResponse = {
  items: Genre[]
  page: number
  pageSize: number
  total: number
}

function App() {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<Genre | null>(null)
  const [editName, setEditName] = useState('')

  const genresQuery = useQuery({
    queryKey: ['genres', search],
    queryFn: async () => {
      const response = await api.get<GenreListResponse>('/api/v1/genres', {
        params: { search: search || undefined, pageSize: 50 },
      })
      return response.data
    },
  })

  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: async () => {
      const response = await api.get<string>('/health/ready', { responseType: 'text' })
      return response.data
    },
    refetchInterval: 15000,
  })

  const createGenre = useMutation({
    mutationFn: async (payload: { name: string }) => {
      const response = await api.post<Genre>('/api/v1/genres', payload)
      return response.data
    },
    onSuccess: () => {
      setName('')
      queryClient.invalidateQueries({ queryKey: ['genres'] })
    },
  })

  const updateGenre = useMutation({
    mutationFn: async (payload: { id: string; name: string }) => {
      const response = await api.put<Genre>(`/api/v1/genres/${payload.id}`, { name: payload.name })
      return response.data
    },
    onSuccess: () => {
      setEditing(null)
      setEditName('')
      queryClient.invalidateQueries({ queryKey: ['genres'] })
    },
  })

  const deleteGenre = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/v1/genres/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['genres'] })
    },
  })

  const healthLabel = useMemo(() => {
    if (healthQuery.isPending) return 'checking'
    if (healthQuery.isError) return 'offline'
    return healthQuery.data?.toLowerCase() === 'healthy' ? 'ready' : 'degraded'
  }, [healthQuery.data, healthQuery.isError, healthQuery.isPending])

  function submitCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    createGenre.mutate({ name })
  }

  function submitEdit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (editing) {
      updateGenre.mutate({ id: editing.id, name: editName })
    }
  }

  function startEditing(genre: Genre) {
    setEditing(genre)
    setEditName(genre.name)
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
      </header>

      <section className="workspace" aria-label="Genre management">
        <aside className="panel">
          <h2>{editing ? 'Edit Genre' : 'New Genre'}</h2>
          {editing ? (
            <form onSubmit={submitEdit} className="form-stack">
              <label htmlFor="edit-name">Name</label>
              <input
                id="edit-name"
                value={editName}
                onChange={(event) => setEditName(event.target.value)}
                maxLength={120}
                autoFocus
              />
              {updateGenre.isError && <ProblemMessage error={updateGenre.error} />}
              <div className="actions">
                <button type="submit" disabled={updateGenre.isPending}>
                  Save
                </button>
                <button type="button" className="secondary" onClick={() => setEditing(null)}>
                  Cancel
                </button>
              </div>
            </form>
          ) : (
            <form onSubmit={submitCreate} className="form-stack">
              <label htmlFor="name">Name</label>
              <input
                id="name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                maxLength={120}
              />
              {createGenre.isError && <ProblemMessage error={createGenre.error} />}
              <button type="submit" disabled={createGenre.isPending}>
                Create
              </button>
            </form>
          )}
        </aside>

        <section className="content-area">
          <div className="section-heading">
            <div>
              <h2>Genres</h2>
              <p>{genresQuery.data?.total ?? 0} active records</p>
            </div>
            <label className="search">
              <span>Search</span>
              <input value={search} onChange={(event) => setSearch(event.target.value)} />
            </label>
          </div>

          {genresQuery.isPending && <p className="state">Loading genres...</p>}
          {genresQuery.isError && <ProblemMessage error={genresQuery.error} />}
          {genresQuery.data?.items.length === 0 && <p className="state">No genres found.</p>}

          <div className="table" role="table" aria-label="Genres">
            {genresQuery.data?.items.map((genre) => (
              <div className="table-row" role="row" key={genre.id}>
                <div>
                  <strong>{genre.name}</strong>
                  {genre.isSystem && <span className="pill">System</span>}
                </div>
                <div className="row-actions">
                  <button
                    type="button"
                    className="secondary"
                    disabled={genre.isSystem}
                    onClick={() => startEditing(genre)}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="danger"
                    disabled={genre.isSystem || deleteGenre.isPending}
                    onClick={() => deleteGenre.mutate(genre.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>
      </section>
    </main>
  )
}

function ProblemMessage({ error }: { error: unknown }) {
  let message = 'Request failed.'

  if (axios.isAxiosError(error)) {
    const detail = error.response?.data?.detail
    const title = error.response?.data?.title
    message = detail ?? title ?? message
  }

  return (
    <p className="problem" role="alert">
      {message}
    </p>
  )
}

export default App
