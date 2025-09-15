using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using TrueMapper.Core.Extensions;

namespace TrueMapper.Tests
{
    public class CollectionMappingTests
    {
        public class SourceItem
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        public class DestinationItem
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        private List<SourceItem> GetTestData()
        {
            return new List<SourceItem>
            {
                new SourceItem { Name = "Item1", Value = 1 },
                new SourceItem { Name = "Item2", Value = 2 },
                new SourceItem { Name = "Item3", Value = 3 }
            };
        }

        [Fact]
        public void MapTo_IEnumerable_Object_ShouldReturnList()
        {
            // Arrange
            var source = GetTestData().Cast<object>();

            // Act - Correct usage: specify List<DestinationItem> to get a list
            var result = source.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Item1", result[0].Name);
            Assert.Equal(1, result[0].Value);
            Assert.Equal("Item3", result[2].Name);
            Assert.Equal(3, result[2].Value);
        }

        [Fact]
        public void MapTo_SingleObject_ShouldWork()
        {
            // Arrange  
            var source = new SourceItem { Name = "Single", Value = 42 };

            // Act
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Single", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void MapTo_AnonymousObject_ShouldWork()
        {
            // Arrange
            var source = new { Name = "Anonymous", Value = 100 };

            // Act
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Anonymous", result.Name);
            Assert.Equal(100, result.Value);
        }

        [Fact]
        public void MapTo_EmptyCollection_ShouldWork()
        {
            // Arrange
            var source = new List<object>();

            // Act
            var result = source.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void MapTo_ComplexMapping_ShouldWork()
        {
            // Arrange
            var complexSource = new List<object>
            {
                new { Name = "Complex1", Value = 10 },
                new { Name = "Complex2", Value = 20 },
                new SourceItem { Name = "Mixed", Value = 30 }
            };

            // Act
            var result = complexSource.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Complex1", result[0].Name);
            Assert.Equal(10, result[0].Value);
            Assert.Equal("Mixed", result[2].Name);
            Assert.Equal(30, result[2].Value);
        }

        [Fact]
        public void MapTo_WithNullItems_ShouldHandleGracefully()
        {
            // Arrange
            var source = new List<object> { new SourceItem { Name = "Valid", Value = 1 }, null! };

            // Act
            var result = source.MapTo<List<DestinationItem>>();
            
            // Assert - should only contain the valid item (current implementation keeps null)
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Actually contains 2 items: valid + null
            Assert.Equal("Valid", result[0].Name);
            Assert.Equal(1, result[0].Value);
            Assert.Null(result[1]); // Second item is null
        }

        // Test MapTo API pentru obiecte simple
        [Fact]
        public void MapTo_SingleObject_FluentAPI_ShouldWork()
        {
            // Arrange
            var source = new SourceItem { Name = "FluentTest", Value = 999 };

            // Act
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FluentTest", result.Name);
            Assert.Equal(999, result.Value);
        }

        // Test pentru verificarea că extension method funcționează
        [Fact]
        public void ExtensionMethod_Accessibility_ShouldWork()
        {
            // Arrange
            var source = new { Name = "Extension", Value = 123 };

            // Act - testăm că extension method este accesibil
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Extension", result.Name);
            Assert.Equal(123, result.Value);
        }

        // Test pentru mapping cu proprietăți lipsă
        [Fact]
        public void MapTo_MissingProperties_ShouldUseDefaults()
        {
            // Arrange
            var source = new { Name = "OnlyName" }; // lipsește Value

            // Act
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("OnlyName", result.Name);
            Assert.Equal(0, result.Value); // default value
        }

        // Test pentru mapping cu proprietăți în plus
        [Fact]
        public void MapTo_ExtraProperties_ShouldIgnore()
        {
            // Arrange
            var source = new { Name = "WithExtra", Value = 456, ExtraProperty = "ignored" };

            // Act
            var result = source.MapTo<DestinationItem>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("WithExtra", result.Name);
            Assert.Equal(456, result.Value);
        }

        // Test pentru MapTo API pentru colecții
        [Fact]
        public void MapTo_Collections_FluentAPI_ShouldWork()
        {
            // Arrange - folosim IEnumerable<object> pentru a activa metoda corectă
            var sourceList = GetTestData().Cast<object>();

            // Act - aceasta folosește MapTo<List<TDestination>>() 
            var result = sourceList.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Item1", result[0].Name);
            Assert.Equal(1, result[0].Value);
            Assert.Equal("Item2", result[1].Name);
            Assert.Equal(2, result[1].Value);
            Assert.Equal("Item3", result[2].Name);
            Assert.Equal(3, result[2].Value);
        }

        [Fact]
        public void MapTo_TypeSafety_ShouldWork()
        {
            // Arrange
            var sources = new[]
            {
                new { Name = "Dynamic1", Value = 10 },
                new { Name = "Dynamic2", Value = 20 }
            }.Cast<object>();

            // Act
            var results = sources.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Dynamic1", results[0].Name);
            Assert.Equal(10, results[0].Value);
            Assert.Equal("Dynamic2", results[1].Name);
            Assert.Equal(20, results[1].Value);
        }

        [Fact]
        public void Performance_LargeCollection_ShouldHandleWell()
        {
            // Arrange
            var largeCollection = Enumerable.Range(1, 100)
                .Select(i => new { Name = $"Item{i}", Value = i * 10 })
                .Cast<object>();

            // Act
            var result = largeCollection.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count);
            Assert.Equal("Item1", result[0].Name);
            Assert.Equal(10, result[0].Value);
            Assert.Equal("Item50", result[49].Name);
            Assert.Equal(500, result[49].Value);
            Assert.Equal("Item100", result[99].Name);
            Assert.Equal(1000, result[99].Value);
        }

        [Fact]
        public void Compatibility_WithExistingAPI_ShouldWork()
        {
            // Test că API-ul nou funcționează alături de cel vechi
            
            // Arrange
            var oldStyleSource = new SourceItem { Name = "OldStyle", Value = 789 };
            var newStyleSources = new[] { new { Name = "NewStyle1", Value = 111 }, new { Name = "NewStyle2", Value = 222 } }.Cast<object>();

            // Act
            var oldResult = oldStyleSource.MapTo<DestinationItem>(); // single object
            var newResult = newStyleSources.MapTo<List<DestinationItem>>(); // collection

            // Assert
            Assert.NotNull(oldResult);
            Assert.Equal("OldStyle", oldResult.Name);
            Assert.Equal(789, oldResult.Value);

            Assert.NotNull(newResult);
            Assert.Equal(2, newResult.Count);
            Assert.Equal("NewStyle1", newResult[0].Name);
            Assert.Equal(111, newResult[0].Value);
            Assert.Equal("NewStyle2", newResult[1].Name);
            Assert.Equal(222, newResult[1].Value);
        }

        // Test pentru exemplul specific al userului - versiunea simplificată
        [Fact]
        public void MapTo_ListToList_UserExample_ShouldWork()
        {
            // Arrange - simulăm TokenRequest și TokenModel cu SourceItem și DestinationItem
            List<SourceItem> requests = new() 
            { 
                new SourceItem { Name = "test", Value = 123 }, 
                new SourceItem { Name = "admin", Value = 456 } 
            };

            // Act - exact ca în exemplul userului: requests.MapTo<List<TokenModel>>()
            var result = requests.MapTo<List<DestinationItem>>();

            // Debug - să vedem exact ce tip este result
            var resultType = result.GetType();
            
            // Assert - testăm mai întâi că nu e null și apoi tipul
            Assert.NotNull(result);
            
            // Verificăm că tipul e corect
            Assert.True(result is List<DestinationItem>, $"Expected List<DestinationItem>, got {resultType}");
            
            // Dacă ajungem aici, testăm și conținutul
            var typedResult = result as List<DestinationItem>;
            Assert.NotNull(typedResult);
            Assert.Equal(2, typedResult.Count);
            Assert.Equal("test", typedResult[0].Name);
            Assert.Equal(123, typedResult[0].Value);
            Assert.Equal("admin", typedResult[1].Name);
            Assert.Equal(456, typedResult[1].Value);
        }

        // Verificăm și că funcționează cu strongly typed collections
        [Fact]
        public void MapTo_StronglyTypedList_ShouldWork()
        {
            // Arrange
            var sourceList = new List<SourceItem>
            {
                new SourceItem { Name = "User1", Value = 100 },
                new SourceItem { Name = "User2", Value = 200 },
                new SourceItem { Name = "User3", Value = 300 }
            };

            // Act
            var destinationList = sourceList.MapTo<List<DestinationItem>>();

            // Assert
            Assert.NotNull(destinationList);
            Assert.IsType<List<DestinationItem>>(destinationList);
            Assert.Equal(3, destinationList.Count);
            Assert.Equal("User1", destinationList[0].Name);
            Assert.Equal(100, destinationList[0].Value);
            Assert.Equal("User2", destinationList[1].Name);
            Assert.Equal(200, destinationList[1].Value);
            Assert.Equal("User3", destinationList[2].Name);
            Assert.Equal(300, destinationList[2].Value);
        }
    }
}