import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import App from './App'

describe('App', () => {
  it('renders the app title', () => {
    render(<App />)
    const titleElement = screen.getByText(/Cash Register/i)
    expect(titleElement).toBeInTheDocument()
  })
})