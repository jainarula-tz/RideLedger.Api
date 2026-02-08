# Accounting Service - API Quickstart Guide

This guide demonstrates the primary API workflows for the Dual-Entry Accounting and Invoicing Service using real request/response examples.

## Prerequisites

- **Base URL**: `https://api.example.com/v1`
- **Authentication**: JWT token in `Authorization: Bearer <token>` header
- **Tenant ID**: Automatically extracted from JWT claims for multi-tenant isolation
- **Content-Type**: `application/json` for all requests

## End-to-End Workflow Example

### 1. Create an Account

Create a receivable account for a customer (e.g., hospital, facility).

**Request:**
```bash
curl -X POST https://api.example.com/v1/accounts \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "City Hospital",
    "accountType": "Receivable",
    "currency": "USD"
  }'
```

**Response (201 Created):**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountNumber": "ACC-1001",
  "name": "City Hospital",
  "accountType": "Receivable",
  "currency": "USD",
  "currentBalance": 0.00,
  "status": "Active",
  "metadata": {},
  "createdAt": "2026-02-01T08:00:00Z",
  "updatedAt": "2026-02-01T08:00:00Z"
}
```

**Key Points:**
- Account starts with zero balance
- `accountId` (UUID) is used in subsequent API calls
- `accountNumber` (ACC-1001) is human-readable identifier
- Status is `Active` by default

---

### 2. Record a Ride Charge (with Idempotency)

Record a billable ride service. Use idempotency key to prevent duplicate charges on retries.

**Request:**
```bash
curl -X POST https://api.example.com/v1/charges \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 45.50,
    "currency": "USD",
    "rideId": "RIDE-2026-02-06-001",
    "rideDate": "2026-02-06",
    "description": "Transport from City Hospital to Outpatient Clinic",
    "idempotencyKey": "charge-ride-2026-02-06-001-v1"
  }'
```

**Response (201 Created):**
```json
{
  "chargeId": "7f9e8d6c-5b4a-4321-8765-1234abcd5678",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 45.50,
  "currency": "USD",
  "rideId": "RIDE-2026-02-06-001",
  "rideDate": "2026-02-06",
  "description": "Transport from City Hospital to Outpatient Clinic",
  "ledgerEntries": [
    {
      "ledgerEntryId": "a1b2c3d4-e5f6-7a8b-9c1d-2e3f4a5b6c7d",
      "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "entryType": "Debit",
      "amount": 45.50,
      "balance": 45.50
    },
    {
      "ledgerEntryId": "b2c3d4e5-f6a7-8b9c-1d2e-3f4a5b6c7d8e",
      "accountId": "<system-revenue-account>",
      "entryType": "Credit",
      "amount": 45.50,
      "balance": -45.50
    }
  ],
  "recordedAt": "2026-02-06T14:30:00Z"
}
```

**Key Points:**
- **Double-Entry Accounting**: Creates paired debit (customer owes $45.50) and credit (revenue earned $45.50) entries
- **Idempotency**: Retrying with same `idempotencyKey` returns `200 OK` with same response (no duplicate charge)
- **Balance Updated**: Account balance increases from $0.00 → $45.50
- **Performance**: < 100ms latency for charge recording

**Retry Example (Duplicate Prevention):**
```bash
# Retry with same idempotencyKey returns 200 OK (not 201)
curl -X POST https://api.example.com/v1/charges \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 45.50,
    "idempotencyKey": "charge-ride-2026-02-06-001-v1",
    ...
  }'
```

**Response (200 OK):**
```json
{
  "chargeId": "7f9e8d6c-5b4a-4321-8765-1234abcd5678",
  ...
  "recordedAt": "2026-02-06T14:30:00Z"
}
```
*(Same response, no duplicate charge created)*

---

### 3. Check Real-Time Account Balance

Retrieve the current outstanding balance for an account.

**Request:**
```bash
curl -X GET "https://api.example.com/v1/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/balance" \
  -H "Authorization: Bearer <jwt-token>"
```

**Response (200 OK):**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "currentBalance": 45.50,
  "currency": "USD",
  "lastUpdated": "2026-02-06T14:30:00Z"
}
```

**Key Points:**
- Balance is computed in real-time from ledger entries
- Reflects all charges and payments up to request time
- < 50ms latency for balance retrieval

---

### 4. Record Multiple Charges Over Time

Record additional ride charges during the billing period.

**Charge 2:**
```bash
curl -X POST https://api.example.com/v1/charges \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 62.75,
    "currency": "USD",
    "rideId": "RIDE-2026-02-15-003",
    "rideDate": "2026-02-15",
    "idempotencyKey": "charge-ride-2026-02-15-003-v1"
  }'
```

**New Balance: $108.25** ($45.50 + $62.75)

---

### 5. Record a Payment

Record a payment received from the customer.

**Request:**
```bash
curl -X POST https://api.example.com/v1/payments \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 50.00,
    "currency": "USD",
    "paymentMethod": "Card",
    "paymentDate": "2026-02-20",
    "referenceNumber": "PAY-20260220-4532",
    "idempotencyKey": "payment-2026-02-20-4532-v1"
  }'
```

**Response (201 Created):**
```json
{
  "paymentId": "9e1f2a3b-5d4c-6e7f-8a9b-1c2d3e4f5a6b",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 50.00,
  "currency": "USD",
  "paymentMethod": "Card",
  "paymentDate": "2026-02-20",
  "referenceNumber": "PAY-20260220-4532",
  "ledgerEntries": [
    {
      "ledgerEntryId": "c3d4e5f6-a7b8-9c1d-2e3f-4a5b6c7d8e9f",
      "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "entryType": "Credit",
      "amount": 50.00,
      "balance": 58.25
    },
    {
      "ledgerEntryId": "d4e5f6a7-b8c9-1d2e-3f4a-5b6c7d8e9f1a",
      "accountId": "<system-cash-account>",
      "entryType": "Debit",
      "amount": 50.00,
      "balance": 50.00
    }
  ],
  "recordedAt": "2026-02-20T16:45:00Z"
}
```

**Key Points:**
- **Double-Entry**: Payment creates paired credit (reduces customer balance) and debit (cash received)
- **Balance Updated**: $108.25 - $50.00 = **$58.25 outstanding**
- **Idempotency**: 24-hour window for duplicate prevention

---

### 6. Generate Monthly Invoice

Generate an invoice for all charges/payments in February 2026.

**Request:**
```bash
curl -X POST https://api.example.com/v1/invoices/generate \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "billingFrequency": "Monthly",
    "billingPeriodStart": "2026-02-01",
    "billingPeriodEnd": "2026-02-28"
  }'
```

**Response (201 Created):**
```json
{
  "invoiceId": "9f8e7d6c-5b4a-4321-8765-4321abcd1234",
  "invoiceNumber": "INV-2026-0042",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountName": "City Hospital",
  "billingPeriodStart": "2026-02-01",
  "billingPeriodEnd": "2026-02-28",
  "generatedAt": "2026-03-01T09:00:00Z",
  "currency": "USD",
  "lineItems": [
    {
      "lineItemId": "a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d",
      "rideId": "RIDE-2026-02-06-001",
      "serviceDate": "2026-02-06",
      "description": "Transportation service - Ride RIDE-2026-02-06-001",
      "amount": 45.50,
      "ledgerEntryIds": [
        "a1b2c3d4-e5f6-7a8b-9c1d-2e3f4a5b6c7d",
        "b2c3d4e5-f6a7-8b9c-1d2e-3f4a5b6c7d8e"
      ]
    },
    {
      "lineItemId": "b2c3d4e5-f6a7-4b5c-1d2e-8f7a6b5c4d3e",
      "rideId": "RIDE-2026-02-15-003",
      "serviceDate": "2026-02-15",
      "description": "Transportation service - Ride RIDE-2026-02-15-003",
      "amount": 62.75,
      "ledgerEntryIds": [
        "e5f6a7b8-c9d1-2e3f-4a5b-6c7d8e9f1a2b",
        "f6a7b8c9-d1e2-3f4a-5b6c-7d8e9f1a2b3c"
      ]
    }
  ],
  "subtotal": 108.25,
  "totalPaymentsApplied": 50.00,
  "outstandingBalance": 58.25,
  "status": "Generated"
}
```

**Key Points:**
- **Flexible Billing**: Supports PerRide, Daily, Weekly, Monthly frequencies
- **Traceability**: Each line item references ledger entry IDs for audit trail
- **Balance Calculation**: Subtotal ($108.25) - Payments ($50.00) = Outstanding ($58.25)
- **Performance**: < 2 seconds for invoice generation (up to 1000 line items)
- **Immutability**: Once generated, invoices cannot be modified

---

### 7. Retrieve Invoice Details

**Request:**
```bash
curl -X GET "https://api.example.com/v1/invoices/9f8e7d6c-5b4a-4321-8765-4321abcd1234" \
  -H "Authorization: Bearer <jwt-token>"
```

**Response:** *(Same as generation response)*

---

### 8. List Account Charges (Audit Trail)

Retrieve all charges for an account for audit purposes.

**Request:**
```bash
curl -X GET "https://api.example.com/v1/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/charges?startDate=2026-02-01&endDate=2026-02-28" \
  -H "Authorization: Bearer <jwt-token>"
```

**Response (200 OK):**
```json
{
  "charges": [
    {
      "chargeId": "7f9e8d6c-5b4a-4321-8765-1234abcd5678",
      "amount": 45.50,
      "rideId": "RIDE-2026-02-06-001",
      "rideDate": "2026-02-06",
      "recordedAt": "2026-02-06T14:30:00Z"
    },
    {
      "chargeId": "8a1b2c3d-4e5f-6789-abcd-ef1234567890",
      "amount": 62.75,
      "rideId": "RIDE-2026-02-15-003",
      "rideDate": "2026-02-15",
      "recordedAt": "2026-02-15T11:20:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 50
}
```

---

## Alternative Billing Frequencies

### Per-Ride Invoice (One Invoice per Ride)

```bash
curl -X POST https://api.example.com/v1/invoices/generate \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "billingFrequency": "PerRide",
    "rideIds": ["RIDE-2026-02-06-001"]
  }'
```

**Response:** Invoice with single line item for RIDE-2026-02-06-001 ($45.50)

---

### Weekly Invoice

```bash
curl -X POST https://api.example.com/v1/invoices/generate \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "billingFrequency": "Weekly",
    "billingPeriodStart": "2026-02-01",
    "billingPeriodEnd": "2026-02-07"
  }'
```

**Response:** Invoice with charges from Feb 1-7 only

---

## Error Handling Examples

### Validation Error (400 Bad Request)

**Request (Invalid Amount):**
```bash
curl -X POST https://api.example.com/v1/charges \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": -10.00,
    "currency": "USD",
    "rideId": "RIDE-2026-02-06-001"
  }'
```

**Response (400 Bad Request):**
```json
{
  "type": "https://api.example.com/problems/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "instance": "/v1/charges",
  "errors": {
    "amount": ["Amount must be greater than 0"]
  }
}
```

---

### Account Not Found (404 Not Found)

**Request:**
```bash
curl -X GET "https://api.example.com/v1/accounts/00000000-0000-0000-0000-000000000000/balance" \
  -H "Authorization: Bearer <jwt-token>"
```

**Response (404 Not Found):**
```json
{
  "type": "https://api.example.com/problems/account-not-found",
  "title": "Account Not Found",
  "status": 404,
  "detail": "Account with ID 00000000-0000-0000-0000-000000000000 not found",
  "instance": "/v1/accounts/00000000-0000-0000-0000-000000000000/balance"
}
```

---

### Duplicate Idempotency Key (409 Conflict)

**Request (Same key, different amount):**
```bash
curl -X POST https://api.example.com/v1/charges \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "amount": 99.99,
    "idempotencyKey": "charge-ride-2026-02-06-001-v1"
  }'
```

**Response (409 Conflict):**
```json
{
  "type": "https://api.example.com/problems/idempotency-conflict",
  "title": "Idempotency Key Conflict",
  "status": 409,
  "detail": "Idempotency key 'charge-ride-2026-02-06-001-v1' already used with different request payload",
  "instance": "/v1/charges"
}
```

---

## Performance Characteristics

| Operation | Target Latency | Notes |
|-----------|---------------|-------|
| Create Account | < 50ms | Single write operation |
| Record Charge | < 100ms | Creates 2 ledger entries (debit + credit) |
| Record Payment | < 100ms | Creates 2 ledger entries (credit + debit) |
| Get Balance | < 50ms | Real-time aggregate query |
| Generate Invoice | < 2s | Up to 1000 line items |
| Retrieve Invoice | < 100ms | Single read operation |

---

## Multi-Tenant Isolation

All API requests automatically enforce tenant isolation via JWT claims:

```
Authorization: Bearer <jwt-token>

JWT Payload:
{
  "sub": "user-id-123",
  "tenant_id": "tenant-abc",
  "roles": ["AccountManager"],
  "exp": 1735689600
}
```

**Automatic Filtering:**
- All queries filter by `tenant_id` claim
- Attempting to access another tenant's data returns `404 Not Found` (not `403 Forbidden` to prevent tenant enumeration)
- Database-level Row-Level Security (RLS) policies enforce tenant boundaries

---

## Next Steps

1. **Explore API Contracts**: See detailed schemas in `/contracts/*.yaml`
2. **Review Data Model**: Understand entities/aggregates in `data-model.md`
3. **Architecture Decisions**: Read rationale in `research.md`
4. **Implementation Plan**: Full technical context in `plan.md`

---

## Support

For API issues or questions, contact: **accounting@example.com**
