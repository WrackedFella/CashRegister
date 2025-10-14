import { useState } from 'react'
import { Container, Row, Col, Form, Button, Alert } from 'react-bootstrap'
import './CashRegisterWidget.scss'

function CashRegisterWidget() {
  const [file, setFile] = useState<File | null>(null)
  const [results, setResults] = useState('')
  const [showResults, setShowResults] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
      setError('')
      setShowResults(false)
    }
  }

  const handleSubmit = async () => {
    if (!file) {
      setError('Please select a file first')
      return
    }

    setIsLoading(true)
    setError('')
    setShowResults(false)

    try {
      const formData = new FormData()
      formData.append('file', file)

      const response = await fetch('/cashregister/calculate-change', {
        method: 'POST',
        body: formData,
      })

      if (response.ok) {
        const data = await response.json()
        setResults(data.results)
        setShowResults(true)
      } else {
        const errorData = await response.json().catch(() => ({ error: 'An error occurred' }))
        setError(errorData.error || 'An error occurred while processing the file')
      }
    } catch (err) {
      setError('Network error. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  const handleClear = () => {
    setFile(null)
    setResults('')
    setShowResults(false)
    setError('')
  }

  return (
    <Container id="CashRegisterWidget">
      <Row>
        <Col>
          <h1 className="text-center mb-4">Cash Register</h1>
          <hr />
          <h5 className="text-center mb-4">
            Upload a file with transaction totals (separated by lines) and press Submit to see the results.
          </h5>
          <hr />
          <Form>
            <Form.Group controlId="fileUpload" className="mb-3">
              <Form.Label>File Upload</Form.Label>
              <Form.Control type="file" accept=".txt" onChange={handleFileChange} />
            </Form.Group>
            
            {error && (
              <Alert variant="danger" className="mb-3">
                {error}
              </Alert>
            )}

            {showResults && (
              <Form.Group controlId="results" className="mb-3">
                <Form.Label>Results</Form.Label>
                <Form.Control as="textarea" readOnly value={results} rows={10} />
              </Form.Group>
            )}

            <div className="d-flex gap-2 justify-content-center">
              <Button 
                variant="success" 
                onClick={handleSubmit} 
                disabled={isLoading || !file}
              >
                {isLoading ? 'Processing...' : 'Submit'}
              </Button>
              <Button variant="secondary" onClick={handleClear}>Clear</Button>
            </div>
          </Form>
        </Col>
      </Row>
    </Container>
  )
}

export default CashRegisterWidget