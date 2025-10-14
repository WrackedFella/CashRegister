import { useState } from 'react'
import { Container, Row, Col, Form, Button } from 'react-bootstrap'
import './CashRegisterWidget.scss'

function CashRegisterWidget() {
  const [file, setFile] = useState<File | null>(null)
  const [results, setResults] = useState('')
  const [showResults, setShowResults] = useState(false)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
    }
  }

  const handleSubmit = () => {
    // Placeholder for processing the file
    setResults(`Processing file: ${file ? file.name : 'No file selected'}`)
    setShowResults(true)
  }

  const handleClear = () => {
    setFile(null)
    setResults('')
    setShowResults(false)
  }

  return (
    <Container id="CashRegisterWidget">
      <Row>
        <Col>
          <h1 className="text-center mb-4">Cash Register</h1>
          <hr />
          <h5 className="text-center mb-4">
            Upload a file with transaction totals (seperated by lines) and press Submit to see the results.
          </h5>
          <hr />
          <Form>
            <Form.Group controlId="fileUpload" className="mb-3">
              <Form.Label>File Upload</Form.Label>
              <Form.Control type="file" onChange={handleFileChange} />
            </Form.Group>
            {showResults && (
              <Form.Group controlId="results" className="mb-3">
                <Form.Label>Results</Form.Label>
                <Form.Control as="textarea" readOnly value={results} rows={5} disabled />
              </Form.Group>
            )}
            <div className="d-flex gap-2 justify-content-center">
              <Button variant="success" onClick={handleSubmit}>Submit</Button>
              <Button variant="secondary" onClick={handleClear}>Clear</Button>
            </div>
          </Form>
        </Col>
      </Row>
    </Container>
  )
}

export default CashRegisterWidget