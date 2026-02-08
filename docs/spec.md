# Feature Specification: Dual-Entry Accounting & Invoicing Service

**Feature Branch**: `backend-api`  
**Created**: 2026-02-06  
**Status**: Draft  
**Input**: User description: "Dual-Entry Accounting and Invoicing Service - A financial system of record for billable ride services with double-entry ledger, multi-tenant support, and flexible invoice generation"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Ride Charges to Account Ledger (Priority: P1)

As a system integrator, I need to record completed ride charges to the appropriate account's ledger so that financial obligations are tracked accurately and immutably.

**Why this priority**: This is the foundation of the entire accounting system. Without the ability to record charges, no other functionality (invoicing, payments, balances) can work. This represents the core value proposition.

**Independent Test**: Can be fully tested by creating an account, posting a ride charge, and verifying that two ledger entries are created (debit to Accounts Receivable, credit to Service Revenue) with correct amounts and metadata. Delivers immediate value by establishing financial records.

**Acceptance Scenarios**:

1. **Given** a ride has been completed and fare calculated, **When** the system receives a charge request with Ride ID, Account ID, Fare Amount, Service Date, and Fleet ID, **Then** the system creates two ledger entries following double-entry accounting principles and returns success confirmation
2. **Given** an existing ledger entry for a specific Ride ID and Account ID, **When** the system receives another charge request for the same Ride ID and Account ID, **Then** the system rejects the duplicate charge and returns an idempotency error
3. **Given** a charge request with invalid Account ID, **When** the system processes the request, **Then** the system rejects the charge and returns an account not found error
4. **Given** multiple concurrent charge requests for different rides, **When** all requests are processed, **Then** all ledger entries are correctly recorded without data corruption

---

### User Story 2 - Calculate and Retrieve Account Balance (Priority: P1)

As a billing administrator, I need to view the current outstanding balance for any account so that I can understand financial obligations at any point in time.

**Why this priority**: Balance calculation is essential for understanding financial position. This is required before generating invoices or making payment decisions. Ties directly to core business goal of tracking receivables.

**Independent Test**: Can be fully tested by creating an account, posting several charges and payments, then querying the balance. The balance calculation (Total Debits - Total Credits) should accurately reflect all transactions. Delivers value by providing immediate financial visibility.

**Acceptance Scenarios**:

1. **Given** an account with posted charges totaling $500, **When** the balance is requested, **Then** the system returns a balance of $500
2. **Given** an account with charges of $500 and payments of $300, **When** the balance is requested, **Then** the system returns a balance of $200
3. **Given** an account with charges of $500 and payments of $600 (overpayment), **When** the balance is requested, **Then** the system returns a balance of -$100 (credit balance)
4. **Given** an account with no transactions, **When** the balance is requested, **Then** the system returns a balance of $0

---

### User Story 3 - Record Payments Against Account (Priority: P2)

As a payment processor integration, I need to record received payments against an account so that the Accounts Receivable balance is reduced and payment history is tracked.

**Why this priority**: Payments are critical for closing the financial cycle, but the system can function temporarily with just charges recorded. This enables reconciliation and balance reduction.

**Independent Test**: Can be fully tested by creating an account with existing charges, posting a payment, and verifying that payment ledger entries are created (debit to Cash/Bank, credit to Accounts Receivable) and the balance is reduced accordingly. Delivers value by tracking cash flow and reducing receivables.

**Acceptance Scenarios**:

1. **Given** an account with outstanding balance of $500, **When** a payment of $300 is recorded with Payment Reference ID, Account ID, Amount, and Payment Date, **Then** the system creates payment ledger entries and reduces the balance to $200
2. **Given** an account with balance of $200, **When** a payment of $300 is recorded (overpayment), **Then** the system accepts the payment and the balance becomes -$100 (credit balance)
3. **Given** a payment has already been recorded for a specific Payment Reference ID, **When** the system receives another payment request with the same Reference ID, **Then** the system rejects the duplicate payment and returns an idempotency error
4. **Given** a payment request with invalid Account ID, **When** the system processes the request, **Then** the system rejects the payment and returns an account not found error

---

### User Story 4 - Generate Invoices On-Demand (Priority: P2)

As a billing administrator, I need to generate invoices for any account with flexible billing periods (per ride, daily, weekly, monthly) so that customers receive accurate billing statements reflecting their charges and payments.

**Why this priority**: Invoicing is essential for customer communication and payment collection, but the ledger can exist without invoices being generated. This builds on top of the charge recording foundation.

**Independent Test**: Can be fully tested by creating an account with charges and payments over a date range, requesting invoice generation with a specific frequency, and verifying the invoice contains correct line items, amounts, and balance. Delivers value by providing customer-facing billing documents.

**Acceptance Scenarios**:

1. **Given** an account with 5 rides completed in January, **When** a monthly invoice is requested for January, **Then** the system generates an invoice containing all 5 rides as line items with individual fares, subtotal, applied payments, and outstanding balance
2. **Given** an account with rides on specific dates, **When** a per-ride invoice is requested for a specific Ride ID, **Then** the system generates an invoice containing only that single ride with fare and balance
3. **Given** an account with rides across 7 days, **When** a weekly invoice is requested for that week, **Then** the system generates an invoice containing all rides from that 7-day period
4. **Given** an account with no charges in the requested date range, **When** an invoice is requested, **Then** the system returns an error indicating no billable items found
5. **Given** an invoice has been generated, **When** viewing the invoice details, **Then** each line item references specific ledger entry IDs ensuring full traceability

---

### User Story 5 - Create and Manage Accounts (Priority: P3)

As a system administrator, I need to create and manage accounts for organizations and individuals so that charges and payments can be associated with the correct financial entity.

**Why this priority**: While foundational, account creation is a one-time setup activity. The system needs accounts to function, but this can be done via setup scripts initially. Lower priority than core transaction recording.

**Independent Test**: Can be fully tested by creating accounts with different types (Organization, Individual), verifying uniqueness, and confirming that each account initializes with an empty ledger and zero balance. Delivers value by establishing financial entities.

**Acceptance Scenarios**:

1. **Given** valid account details including Account ID, Name, Type (Organization or Individual), and Status (Active/Inactive), **When** a create account request is submitted, **Then** the system creates the account with an empty ledger and zero balance in USD
2. **Given** an existing Account ID, **When** a create account request is submitted with the same Account ID, **Then** the system rejects the request and returns a duplicate account error
3. **Given** an account has been created, **When** retrieving account details, **Then** the system returns Account ID, Name, Type, Status, Currency (USD), current balance, and ledger summary
4. **Given** an inactive account, **When** a charge or payment is attempted, **Then** the system rejects the transaction and returns an account inactive error

---

### User Story 6 - Generate Account Statements for Date Ranges (Priority: P3)

As a billing administrator or customer, I need to generate account statements for any date range showing all transactions so that I can review financial activity and reconcile records.

**Why this priority**: Statements provide transparency and audit capability but are not required for core financial recording. This enhances visibility after the core system is operational.

**Independent Test**: Can be fully tested by creating an account with charges and payments over multiple months, requesting a statement for a specific date range, and verifying it includes opening balance, all transactions in chronological order, and closing balance. Delivers value by providing historical financial view.

**Acceptance Scenarios**:

1. **Given** an account with transactions spanning 3 months, **When** a statement is requested for month 2, **Then** the system returns opening balance at start of month 2, all transactions within month 2, and closing balance at end of month 2
2. **Given** an account with no transactions in the requested date range, **When** a statement is requested, **Then** the system returns opening balance, no transactions, and closing balance (same as opening)
3. **Given** an account with both charges and payments, **When** a statement is requested, **Then** transactions are listed chronologically showing transaction type, date, description, debit/credit amounts, and running balance

---

### Edge Cases

- **Concurrent Transactions**: What happens when multiple charges or payments are posted to the same account simultaneously? System must handle race conditions and ensure ledger integrity.
- **Idempotency Violations**: What happens when duplicate Ride IDs or Payment Reference IDs are submitted despite idempotency checks? System must detect and reject with clear error messages.
- **Zero-Amount Transactions**: What happens when a ride charge or payment has $0.00 amount? System should reject as invalid.
- **Negative Amounts**: What happens when charges or payments have negative amounts? System should reject as invalid.
- **Deleted Accounts**: What happens when attempting to post charges or payments to a deactivated or deleted account? System must reject transactions.
- **Large Volume**: What happens when an account has thousands of transactions and a statement or balance is requested? System must perform efficiently with pagination support.
- **Time Zone Handling**: What happens when Service Date or Payment Date spans time zones? System must consistently store and retrieve dates in UTC.
- **Ledger Entry Overflow**: What happens when account ID or amounts exceed maximum supported values? System must validate and reject gracefully.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow creation of accounts with Account ID, Account Name, Account Type (Organization or Individual), and Status (Active or Inactive)
- **FR-002**: System MUST assign USD as the fixed currency for all accounts with no currency conversion support
- **FR-003**: System MUST initialize each account with an empty ledger and zero balance upon creation
- **FR-004**: System MUST record ride service charges by accepting Ride ID, Account ID, Fare Amount, Service Date, and Fleet ID
- **FR-005**: System MUST create two ledger entries for each ride charge following double-entry accounting: debit to Accounts Receivable and credit to Service Revenue
- **FR-006**: System MUST enforce idempotency for ride charges by rejecting duplicate Ride ID for the same Account ID
- **FR-007**: System MUST record payments by accepting Payment Reference ID, Account ID, Amount, Payment Date, and optional Payment Mode
- **FR-008**: System MUST create two ledger entries for each payment following double-entry accounting: debit to Cash/Bank and credit to Accounts Receivable
- **FR-009**: System MUST enforce idempotency for payments by rejecting duplicate Payment Reference ID
- **FR-010**: System MUST support partial payments, full payments, and overpayments without restriction
- **FR-011**: System MUST calculate account balance using the formula: Balance = Total Debits - Total Credits
- **FR-012**: System MUST support balance retrieval for any account at any point in time
- **FR-013**: System MUST generate invoices on-demand with support for four billing frequencies: per ride, daily, weekly, and monthly
- **FR-014**: System MUST include in each invoice: unique invoice number, account details, billing period, line items with Ride ID/Service Date/Fare, subtotal, applied payments, and outstanding balance
- **FR-015**: System MUST ensure every invoice line item references specific ledger entry IDs for traceability
- **FR-016**: System MUST make invoices immutable once generated (read-only)
- **FR-017**: System MUST generate account statements for any date range including opening balance, all transactions chronologically, and closing balance
- **FR-018**: System MUST make ledger entries immutable (append-only, no modification or deletion)
- **FR-019**: System MUST store audit metadata for each ledger entry: source type (Ride or Payment), source reference ID, created timestamp, and created by
- **FR-020**: System MUST enforce multi-tenant isolation ensuring ledger data cannot be accessed across tenants
- **FR-021**: System MUST reject transactions with zero or negative amounts
- **FR-022**: System MUST reject transactions for inactive accounts
- **FR-023**: System MUST use fixed-point arithmetic for all monetary calculations to avoid rounding errors
- **FR-024**: System MUST store all dates in UTC to avoid time zone ambiguity
- **FR-025**: System MUST generate unique invoice numbers per tenant following a sequential or timestamp-based pattern

### Key Entities

- **Account**: Represents the financially responsible entity (Organization or Individual). Contains Account ID (unique identifier), Account Name, Account Type (Organization/Individual), Status (Active/Inactive), Currency (fixed to USD),Current Balance (calculated field), and collection of LedgerEntry entities. Owns all financial transactions and invoices.

- **LedgerEntry**: Represents a single line in the double-entry accounting ledger. Contains Entry ID (unique identifier), Account ID (foreign reference), Ledger Account Type (Accounts Receivable, Service Revenue, Cash/Bank), Debit Amount (or null), Credit Amount (or null), Transaction Date, Source Type (Ride or Payment), Source Reference ID (Ride ID or Payment Reference ID), Created Timestamp, and Created By. Immutable once created.

- **Invoice**: Represents a billing statement for an account. Contains Invoice Number (unique per tenant), Account ID (foreign reference), Billing Period Start Date, Billing Period End Date, Invoice Generated Date, Line Items (collection of invoice line items), Subtotal (sum of all line item amounts), Total Payments Applied (sum of payments in period), Outstanding Balance, and Status (Generated). Immutable once created.

- **Invoice Line Item**: Represents a single charge on an invoice. Contains Line Item ID, Invoice Number (foreign reference), Ride ID, Service Date, Fare Amount, Description, and Ledger Entry IDs (references to the specific ledger entries for traceability).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ledger accuracy must be 100% with every charge and payment correctly recording debit and credit entries that balance
- **SC-002**: System must record ride charges with append latency under 100 milliseconds (p95)
- **SC-003**: System must generate invoices on-demand with latency under 2 seconds regardless of transaction volume
- **SC-004**: System must achieve zero duplicate charge incidents through idempotency enforcement
- **SC-005**: System must maintain 100% ledger to invoice traceability with every invoice line item referencing specific ledger entry IDs
- **SC-006**: System must enforce tenant data isolation with zero cross-tenant data leakage incidents
- **SC-007**: System must support at least 10,000 accounts per tenant without performance degradation
- **SC-008**: System must handle at least 1,000 concurrent charge/payment recording requests without data corruption
- **SC-009**: Balance calculation queries must return results in under 50 milliseconds (p95) for accounts with up to 10,000 transactions  
- **SC-010**: Account statement generation must complete in under 3 seconds for date ranges spanning 1 year of transactions
- **SC-011**: System must achieve 99.9% uptime for financial recording operations
- **SC-012**: All monetary calculations must use fixed-point arithmetic with zero floating-point rounding errors

## Scope

### In Scope

- Dual-entry ledger engine with append-only, immutable ledger entries
- Account creation and management for organizations and individuals
- Ride service charge recording with idempotency enforcement
- Payment recording with support for partial, full, and overpayments
- Real-time balance computation from ledger entries
- On-demand invoice generation with multiple billing frequencies (per ride, daily, weekly, monthly)
- Account statement generation for any date range
- Multi-tenant data isolation
- Audit metadata tracking for all financial transactions
- Fixed-point monetary calculations
- UTC-based date/time storage

### Out of Scope

- Ride lifecycle management (creation, assignment, completion tracking)
- Fare calculation logic and pricing rules
- Payment gateway integration and payment processing
- Tax handling (GST, VAT, sales tax calculations)
- Manual adjustments, credits, or refunds
- Fleet payout calculations and disbursements
- Currency conversion or multi-currency support
- Financial reporting and analytics dashboards
- Accounts payable functionality
- Budget management or forecasting
- Integration with external ERP systems

## Assumptions

- Ride completion and fare calculation are handled by upstream systems that will trigger charge recording via API
- Payment processing is handled by external payment gateways that will notify this system when payments are received
- Account creation will be triggered by user onboarding flows in other systems
- All monetary amounts will be in USD with 2 decimal places
- Tenant ID will be provided in all API requests via authentication context
- System will have read access to ride metadata (Ride ID, Service Date, Fleet ID) but does not own this data
- Invoice numbers will use a tenant-scoped sequential pattern (e.g., INV-00001, INV-00002)
- Audit trail for "created by" will use user/service identifiers from the authentication system
- Balance queries will be real-time calculations, not cached/denormalized values
- Invoice generation is on-demand only; no automated scheduled invoice generation in v1.0
- No retroactive editing or deletion of ledger entries will ever be required
- System will not handle multi-currency accounts or exchange rate conversions in v1.0

## Dependencies

- **Ride Management Service**: Provides Ride ID, Service Date, Fleet ID, and Fare Amount when ride is completed
- **Payment Gateway Service**: Provides Payment Reference ID, Amount, Payment Date when payment is received
- **User Management Service**: Provides Account ID, Account Name, Account Type for account creation
- **Authentication Service**: Provides Tenant ID and User/Service ID for audit trails
- **Notification Service** (optional): May consume invoice generated events to send invoices to customers

## Non-Functional Requirements

- **Performance**: Charge/payment recording must complete in <100ms (p95); Balance queries <50ms (p95); Invoice generation <2 seconds
- **Consistency**: Strong consistency required for ledger entries; All debits must equal credits at all times
- **Scalability**: Must support horizontal scaling for read queries; Write operations scale vertically per tenant
- **Availability**: 99.9% uptime target; Financial operations are critical path
- **Security**: Tenant isolation mandatory; All API endpoints require authentication; Audit logging for all write operations
- **Data Integrity**: Immutable ledger entries; Idempotency for all writes; Fixed-point arithmetic for all monetary calculations
- **Observability**: Structured logging with correlation IDs; Metrics for ledger write latency, balance query latency, invoice generation latency; Distributed tracing for cross-service calls
