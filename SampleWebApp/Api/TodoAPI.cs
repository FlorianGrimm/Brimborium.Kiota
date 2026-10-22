using System.Collections.Concurrent;

namespace SampleWebApp.Api;

public class TodoAPI : IMinimalApiEndpoint
{
    public void MapMinimalApi(WebApplication builder)
    {
        ConcurrentDictionary<int, Todo> dictTodo = new();
        {
            Todo[] sampleTodos = [
                new(1, "Walk the dog"),
            new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
            new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
            new(4, "Clean the bathroom"),
            new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
            ];
            foreach (var todo in sampleTodos)
            {
                dictTodo.TryAdd(todo.Id, todo);
            }
        }

        var group = builder.MapGroup("api").MapGroup("Todo");
        group.MapGet("/", () =>
        {
            return dictTodo.Values.ToList();
        }).WithDisplayName("GetTodos");

        group.MapPut("/{id}", (int id, Todo todo) =>
        {
            if (dictTodo.TryGetValue(id, out var existingTodo))
            {
                var updatedTodo = existingTodo with
                {
                    Title = todo.Title,
                    DueBy = todo.DueBy,
                    IsComplete = todo.IsComplete
                };
                dictTodo[id] = updatedTodo;
                return Results.Ok(updatedTodo);
            }
            return Results.NotFound();
        }).WithDisplayName("UpdateTodo");

        group.MapPost("/", (Todo todo) =>
        {
            if (dictTodo.ContainsKey(todo.Id))
            {
                return Results.Conflict($"Todo with Id {todo.Id} already exists.");
            }
            dictTodo[todo.Id] = todo;
            return Results.Created($"/api/Todo/{todo.Id}", todo);
        }).WithDisplayName("CreateTodo");


        group.MapDelete("/{id}", (int id) =>
        {
            if (dictTodo.TryRemove(id, out var todo))
            {
                return Results.Ok(todo);
            }
            return Results.NotFound();
        }).WithDisplayName("DeleteTodo");
    }
}
/*
Todo[] sampleTodos =
[
    new(1, "Walk the dog"),
new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
new(4, "Clean the bathroom"),
new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
];

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos)
            .WithName("GetTodos");

todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound>(int id) =>
        sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound())
        .WithName("GetTodoById");
*/

