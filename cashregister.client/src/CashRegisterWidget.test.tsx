import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import CashRegisterWidget from './CashRegisterWidget'

// Mock fetch globally
const mockFetch = vi.fn()
global.fetch = mockFetch

describe('CashRegisterWidget', () => {
  beforeEach(() => {
    mockFetch.mockClear()
  })

  it('button is disabled when no file is selected', () => {
    render(<CashRegisterWidget />)
    const submitButton = screen.getByRole('button', { name: /submit/i })
    expect(submitButton).toBeDisabled()
  })

  it('clears error when file is selected after error', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: () => Promise.resolve({ error: 'Server error' })
    })

    render(<CashRegisterWidget />)
    const fileInput = screen.getByLabelText(/file upload/i)
    const testFile = new File(['content'], 'test.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    const submitButton = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Server error')).toBeInTheDocument()
    })

    // Now select a file again
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    // Error should be cleared
    expect(screen.queryByText('Server error')).not.toBeInTheDocument()
  })

  it('handles successful submission', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ results: '3 quarters,1 dime,3 pennies' })
    })

    render(<CashRegisterWidget />)
    const fileInput = screen.getByLabelText(/file upload/i)
    const testFile = new File(['content'], 'test.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    const submitButton = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByDisplayValue('3 quarters,1 dime,3 pennies')).toBeInTheDocument()
    })
    expect(mockFetch).toHaveBeenCalledWith('/cashregister/calculate-change', expect.any(Object))
  })

  it('handles network error', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'))

    render(<CashRegisterWidget />)
    const fileInput = screen.getByLabelText(/file upload/i)
    const testFile = new File(['content'], 'test.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    const submitButton = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Network error. Please try again.')).toBeInTheDocument()
    })
  })

  it('handles server error', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: () => Promise.resolve({ error: 'Invalid file format' })
    })

    render(<CashRegisterWidget />)
    const fileInput = screen.getByLabelText(/file upload/i)
    const testFile = new File(['content'], 'test.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    const submitButton = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Invalid file format')).toBeInTheDocument()
    })
  })

  it('clears all state when clear is clicked', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ results: 'Test results' })
    })

    render(<CashRegisterWidget />)
    const fileInput = screen.getByLabelText(/file upload/i)
    const testFile = new File(['content'], 'test.txt', { type: 'text/plain' })
    fireEvent.change(fileInput, { target: { files: [testFile] } })

    const submitButton = screen.getByRole('button', { name: /submit/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByDisplayValue('Test results')).toBeInTheDocument()
    })

    const clearButton = screen.getByRole('button', { name: /clear/i })
    fireEvent.click(clearButton)

    // Results should be gone
    expect(screen.queryByDisplayValue('Test results')).not.toBeInTheDocument()
    // File input should be reset (though hard to test directly, we can check submit is disabled)
    expect(submitButton).toBeDisabled()
  })
})