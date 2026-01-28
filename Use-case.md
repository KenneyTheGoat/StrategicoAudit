## Use Case Diagram

```mermaid
flowchart LR
    User[Warehouse User]
    Admin[Admin / Support]

    LoginUser[Login as User]
    LoginAdmin[Login as Admin]

    AdjustInventory[Adjust Inventory]
    InventoryUpdated[Inventory Updated]
    AuditLogged[Audit Event Logged]

    ViewAudit[View Audit Log]
    QueryUser[Query User Actions Last Week]
    QueryRecord[Query Record Change History]

    User --> LoginUser --> AdjustInventory
    AdjustInventory --> InventoryUpdated
    AdjustInventory --> AuditLogged

    Admin --> LoginAdmin --> ViewAudit
    ViewAudit --> QueryUser
    ViewAudit --> QueryRecord
