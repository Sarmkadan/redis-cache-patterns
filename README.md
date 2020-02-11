// existing content ...

## Repository

The `Repository` class provides a basic implementation of the generic repository pattern. It allows you to perform CRUD (Create, Read, Update, Delete) operations on a collection of objects.

### Usage Example
```csharp
var repository = new Repository<MyEntity>();

// Get all entities
var entities = await repository.GetAllAsync();
Console.WriteLine($"Entities: {entities.Count()}");

// Get an entity by ID
var entity = await repository.GetByIdAsync(1);
Console.WriteLine($"Entity: {entity?.Name}");

// Add a new entity
var newEntity = new MyEntity { Name = "New Entity" };
await repository.AddAsync(newEntity);
Console.WriteLine($"Added entity: {newEntity.Id}");

// Update an existing entity
entity.Name = "Updated Entity";
await repository.UpdateAsync(entity);
Console.WriteLine($"Updated entity: {entity.Name}");

// Delete an entity
await repository.DeleteAsync(1);
Console.WriteLine($"Deleted entity: 1");

// Count all entities
var count = await repository.CountAsync();
Console.WriteLine($"Count: {count}");

// Check if an entity exists
var exists = await repository.ExistsAsync(1);
Console.WriteLine($"Exists: {exists}");
