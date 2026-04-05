# SQL Editor Guide

## Overview

The SQL Editor is an editor tool that lets you query and update table data using SQL-like syntax.

**Menu**: `Window > XScriptableDB > SQL Editor`

## Basic Operations

- **Execute**: F5 or Ctrl+Enter
- **History**: Ctrl+↑/↓ to recall previous queries
- **Insert Table**: Use the **Insert Table** toolbar button to insert a table name

## SELECT Statement

### Basic Syntax

```sql
SELECT columns FROM table_name [WHERE conditions] [ORDER BY columns] [LIMIT n] [OFFSET n]
```

### Select All Columns

```sql
SELECT * FROM ItemTable
```

### Select Specific Columns

```sql
SELECT Id, Name, Price FROM ItemTable
```

### Column Aliases

```sql
SELECT Id, Name AS ItemName FROM ItemTable
```

### WHERE Clause

```sql
-- Equality
SELECT * FROM ItemTable WHERE Category = 'Weapon'

-- Numeric comparison
SELECT * FROM ItemTable WHERE Price > 1000

-- String comparison (case-insensitive)
SELECT * FROM ItemTable WHERE Name = 'iron sword'
```

### Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equal | `Price = 100` |
| `!=` or `<>` | Not equal | `Price != 0` |
| `<` | Less than | `Price < 100` |
| `<=` | Less than or equal | `Price <= 100` |
| `>` | Greater than | `Price > 100` |
| `>=` | Greater than or equal | `Price >= 100` |

### LIKE Operator

```sql
-- Contains (% matches any sequence of characters)
SELECT * FROM ItemTable WHERE Name LIKE '%sword%'

-- Starts with
SELECT * FROM ItemTable WHERE Name LIKE 'Iron%'

-- Ends with
SELECT * FROM ItemTable WHERE Name LIKE '%Sword'

-- Single character wildcard (_)
SELECT * FROM ItemTable WHERE Name LIKE 'Iron_'
```

### IN Operator

```sql
SELECT * FROM ItemTable WHERE Id IN (1001, 1002, 1003)
SELECT * FROM ItemTable WHERE Category IN ('Weapon', 'Armor')
```

### IS NULL / IS NOT NULL

```sql
SELECT * FROM ItemTable WHERE Description IS NULL
SELECT * FROM ItemTable WHERE Description IS NOT NULL
```

### Logical Operators

```sql
-- AND
SELECT * FROM ItemTable WHERE Category = 'Weapon' AND Price > 1000

-- OR
SELECT * FROM ItemTable WHERE Category = 'Weapon' OR Category = 'Armor'

-- Precedence with parentheses
SELECT * FROM ItemTable WHERE (Category = 'Weapon' OR Category = 'Armor') AND Price > 500
```

### ORDER BY Clause

```sql
-- Ascending (default)
SELECT * FROM ItemTable ORDER BY Price ASC

-- Descending
SELECT * FROM ItemTable ORDER BY Price DESC

-- Multiple columns
SELECT * FROM ItemTable ORDER BY Category ASC, Price DESC
```

### LIMIT / OFFSET

```sql
-- Top 10 records
SELECT * FROM ItemTable LIMIT 10

-- Records 11–20 (pagination)
SELECT * FROM ItemTable LIMIT 10 OFFSET 10

-- Combined with ORDER BY
SELECT * FROM ItemTable ORDER BY Price DESC LIMIT 5
```

## UPDATE Statement

### Basic Syntax

```sql
UPDATE table_name SET column = value [, column = value ...] [WHERE conditions]
```

### Update a Single Column

```sql
UPDATE ItemTable SET Price = 500 WHERE Id = 1001
```

### Update Multiple Columns

```sql
UPDATE ItemTable SET Price = 500, Attack = 30 WHERE Id = 1001
```

### Conditional Bulk Update

```sql
-- Double the price of all Weapon-category items
UPDATE ItemTable SET Price = Price * 2 WHERE Category = 'Weapon'
```

### Warning: Update Without WHERE

```sql
-- All records will be updated!
UPDATE ItemTable SET IsActive = 1
```

## DELETE Statement

### Basic Syntax

```sql
DELETE FROM table_name [WHERE conditions]
```

### Conditional Delete

```sql
DELETE FROM ItemTable WHERE Id = 1001
DELETE FROM ItemTable WHERE Price = 0
DELETE FROM ItemTable WHERE Category = 'Deprecated'
```

### Warning: Delete Without WHERE

```sql
-- All records will be deleted!
DELETE FROM ItemTable
```

## Literals

### Strings

```sql
WHERE Name = 'item name'
WHERE Name = "item name"
WHERE Name = 'It''s a test'  -- escaped quote
```

### Numbers

```sql
WHERE Id = 1001      -- integer
WHERE Rate = 1.5     -- decimal
WHERE Modifier = -10 -- negative
```

### Boolean

```sql
WHERE IsActive = 1  -- true
WHERE IsActive = 0  -- false
```

### NULL

```sql
WHERE Value = NULL  -- Always false (use IS NULL instead)
WHERE Value IS NULL -- Correct usage
```

## Table Names

Table names correspond to the ScriptableObject asset names and are case-insensitive.

```sql
SELECT * FROM ItemTable
SELECT * FROM itemtable  -- same result
```

## Execution Results

### SELECT
- Results displayed in table format (up to 1000 rows)
- Execution time (ms) displayed

### UPDATE / DELETE
- Number of affected records displayed
- Execution time (ms) displayed

## Error Messages

| Error | Cause |
|-------|-------|
| `Table not found: XXX` | The specified table does not exist |
| `Parse error: Expected XXX` | SQL syntax error |
| `Execution error: XXX` | Runtime error |

## Tips

### Performance

```sql
-- Filtering on a SecondaryKey column is faster
SELECT * FROM ItemTable WHERE Category = 'Weapon'

-- Use LIMIT to restrict the result set
SELECT * FROM ItemTable WHERE Price > 0 LIMIT 100
```

### Preview Before Modifying

```sql
-- 1. Confirm target records
SELECT * FROM ItemTable WHERE Price = 0

-- 2. Delete if confirmed
DELETE FROM ItemTable WHERE Price = 0
```
