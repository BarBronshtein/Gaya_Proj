# 06 Angular Client UI
Type: task
Status: ready-for-agent
Blocked by: 05

## Question
How should the Angular client provide an intuitive calculation experience and live operation management proving zero-code extensibility?

### Scope & Acceptance Criteria
- Standalone component architecture in modern Angular with signals/reactive forms.
- View 1: Calculator Screen:
  - Dynamic dropdown populated from `GET /api/operations`.
  - Contextual labels/placeholders for Field A and Field B based on selected operation (e.g. "Latitude" and "Longitude" for weather, "Number 1" and "Number 2" for arithmetic).
  - "חשב" (Calculate) button with loading state.
  - Result panel displaying formatted output.
  - Bonus Section 1: Badge showing count of executions of this operation type this month.
  - Bonus Section 2: Table / cards showing details of the 3 most recent executions of this operation type.
- View 2: Operations Hub (Admin/Extensibility):
  - List of all operations with their rules, category, and status.
  - Interactive form to add a new operation at runtime (e.g. a new string or arithmetic rule).
  - Verify that newly added operations appear immediately in the Calculator dropdown without server restart.
- Clean, responsive styling (Tailwind CSS or clean modern CSS).
