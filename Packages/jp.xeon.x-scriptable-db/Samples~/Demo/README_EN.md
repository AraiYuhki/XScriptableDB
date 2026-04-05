# XScriptableDB Demo

An interactive demo sample showcasing the major features of XScriptableDB.

## Setup

### 1. Create Table Assets

1. Right-click in the Project window
2. Select `Create > XScriptableDB > Samples > DemoItemTable`
3. Select the created asset and import `DemoItems.csv` via the Inspector

### 2. Create a Scene

1. Create a new scene
2. Create an empty GameObject and attach the `DemoManager` script
3. Assign the created table asset to the `Item Table` field on `DemoManager`

### 3. UI (Optional)

For a better experience, it is recommended to create the following UI:

```
Canvas
├── Panel (Left)
│   ├── Button - "Show All Records"      → ShowAllRecords()
│   ├── Button - "PrimaryKey Search"     → SearchByPrimaryKey()
│   ├── Button - "SecondaryKey Search"   → SearchBySecondaryKey()
│   ├── Button - "Composite Key Search"  → SearchByCompositeKey()
│   ├── Button - "Where Search"          → SearchWithWhere()
│   ├── Button - "Range Search"          → SearchInRange()
│   ├── Button - "Aggregation"           → ShowAggregation()
│   └── Button - "Performance Compare"   → ShowPerformanceComparison()
├── InputField                            → searchInput
└── Panel (Right)
    └── Text (Scroll View)                → outputText
```

### 4. Run

1. Enter Play mode
2. Click each button to explore the features
3. Type values in the InputField to change search conditions

## Search Examples

| Feature | Input Example | Description |
|---------|---------------|-------------|
| PrimaryKey search | `1001` | Search by ID |
| SecondaryKey search | `Weapon` | Search by category |
| Composite key search | `Weapon/3` | Search by category/rarity |
| Where search | `500` | Price >= specified value |
| Range search | `1001-1010` | Search by ID range |

## File Structure

- `DemoManager.cs` - Main demo logic
- `DemoItemRecord.cs` - Record definition (includes composite SecondaryKey)
- `DemoItemTable.cs` - Table definition
- `DemoItems.csv` - Sample data
