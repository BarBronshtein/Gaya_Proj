# 0004: Angular Client Architecture

We decided to build the frontend client using modern Angular with standalone components and reactive state. The client is split into two primary views:
1. Calculator Screen: Provides input controls for Field A, Operation dropdown selector, Field B, Calculate button, immediate result display, recent 3 operations of the same type, and current month execution counter badge.
2. Operations Hub: Provides an interactive interface to view existing operations, add new operations, and edit/toggle operations in real time without recompiling or redeploying the backend.
