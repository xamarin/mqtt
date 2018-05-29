using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class InMemoryRepositorySpec
    {
		[Fact]
		public void when_creating_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();

			repository.Create(new FooStorageObject { Id = "Foo1", Value = 1 });
			repository.Create(new FooStorageObject { Id = "Foo2", Value = 2 });
			repository.Create(new FooStorageObject { Id = "Foo3", Value = 3 });
			repository.Create(new FooStorageObject { Id = "Foo4", Value = 4 });

			Assert.Equal(4, repository.GetAll().Count());
		}

		[Fact]
		public void when_updating_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);

			item.Value = 2;

			repository.Update(item);
			
			Assert.Equal(2, repository.GetAll().First().Value);
		}

		[Fact]
		public void when_deleting_item_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);
			repository.Delete(item.Id);

			Assert.Empty(repository.GetAll());
		}

		[Fact]
		public void when_deleting_item_by_id_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);
			repository.Delete("Foo1");

			Assert.Empty(repository.GetAll());
		}

		[Fact]
		public void when_deleting_item_with_invalid_id_then_does_not_delete()
		{
			var repository = new InMemoryRepository<FooStorageObject>();
			var item = new FooStorageObject { Id = "Foo1", Value = 1 };

			repository.Create(item);
			repository.Delete("Foo2");

			Assert.NotEmpty(repository.GetAll());
		}

		[Fact]
		public void when_checking_existing_items_with_expressions_then_succeeds()
		{
			var repository = new InMemoryRepository<FooStorageObject>();

			repository.Create(new FooStorageObject { Id = "Foo1", Value = 1 });
			repository.Create(new FooStorageObject { Id = "Foo2", Value = 2 });
			repository.Create(new FooStorageObject { Id = "Foo3", Value = 3 });
			repository.Create(new FooStorageObject { Id = "Foo4", Value = 4 });

			Assert.Equal(4, repository.GetAll().Where(x => x.Id.StartsWith("Foo")).Count());
			Assert.True(repository.GetAll().Any(x => x.Id == "Foo1"));
			Assert.True(repository.GetAll().Any(x => x.Value == 4));
			Assert.False(repository.GetAll().Any(x => x.Value == 5));
		}

		[Fact]
		public async Task when_getting_element_by_expression_in_multiple_threads_then_succeeds()
		{
			var count = 100;
			var repository = new InMemoryRepository<FooStorageObject>();
			var bag = new ConcurrentBag<FooStorageObject>();
			var createTasks = new List<Task>();

			for (var i = 1; i < count; i++)
			{
				createTasks.Add(Task.Run(() =>
				{
					var item = new FooStorageObject { Id = $"Foo{i}", Value = i };

					repository.Create(item);
				}));
			}

			await Task.WhenAll(createTasks);

			var random = new Random();

			Parallel.For(fromInclusive: 1, toExclusive: count + 1, body: i => {
				var value = random.Next(minValue: 1, maxValue: count);
				var element = repository.Get($"Foo{value}");

				bag.Add(element);
			});

			Assert.Equal(count, bag.Count);
		}
	}

	class FooStorageObject : IStorageObject
	{
		public string Id { get; set; }

		public int Value { get; set; }
	}
}
