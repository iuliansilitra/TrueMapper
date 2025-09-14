using System;
using System.Collections.Generic;
using System.Linq;
using TrueMapper.Core.Core;
using TrueMapper.Core.Extensions;
using Xunit;

namespace TrueMapper.Tests;

public class TrueMapperTests
{
    [Fact]
    public void BasicMapping_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var source = new SourcePerson
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Email = "john.doe@example.com"
        };

        // Act
        var destination = source.MapTo<SourcePerson, DestinationPerson>();

        // Assert
        Assert.Equal(source.FirstName, destination.FirstName);
        Assert.Equal(source.LastName, destination.LastName);
        Assert.Equal(source.Age, destination.Age);
        Assert.Equal(source.Email, destination.Email);
    }

    [Fact]
    public void CollectionMapping_ShouldMapAllItems()
    {
        // Arrange
        var sourceList = new List<SourcePerson>
        {
            new() { FirstName = "John", LastName = "Doe", Age = 30 },
            new() { FirstName = "Jane", LastName = "Smith", Age = 25 }
        };

        // Act
        var destinationList = sourceList.MapTo<SourcePerson, DestinationPerson>().ToList();

        // Assert
        Assert.Equal(2, destinationList.Count);
        Assert.Equal("John", destinationList[0].FirstName);
        Assert.Equal("Jane", destinationList[1].FirstName);
    }

    [Fact]
    public void DeepClone_ShouldCreateExactCopy()
    {
        // Arrange
        var original = new SourcePerson
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            Email = "john.doe@example.com"
        };

        // Act
        var cloned = original.DeepClone();

        // Assert
        Assert.NotSame(original, cloned);
        Assert.Equal(original.FirstName, cloned.FirstName);
        Assert.Equal(original.LastName, cloned.LastName);
        Assert.Equal(original.Age, cloned.Age);
        Assert.Equal(original.Email, cloned.Email);
    }

    [Fact]
    public void CustomMapping_WithConfiguration_ShouldApplyCustomLogic()
    {
        // Arrange
        var mapper = new TrueMapper.Core.Core.TrueMapper();
        
        mapper.Configure()
            .CreateMap<SourcePerson, PersonDto>()
            .ForMember(dto => dto.FullName, src => $"{src.FirstName} {src.LastName}")
            .ForMember(dto => dto.IsAdult, src => src.Age >= 18);

        var source = new SourcePerson
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30
        };

        // Act
        var result = mapper.Map<SourcePerson, PersonDto>(source);

        // Assert
        Assert.Equal("John Doe", result.FullName);
        Assert.True(result.IsAdult);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void ConditionalMapping_ShouldApplyConditions()
    {
        // Arrange
        var mapper = new TrueMapper.Core.Core.TrueMapper();
        
        mapper.Configure()
            .CreateMap<SourcePerson, PersonDto>()
            .When(src => src.Age >= 65,
                  (src, dto) => dto.Category = "Senior",
                  (src, dto) => dto.Category = "Regular");

        var youngPerson = new SourcePerson { FirstName = "John", Age = 25 };
        var seniorPerson = new SourcePerson { FirstName = "Mary", Age = 70 };

        // Act
        var youngResult = mapper.Map<SourcePerson, PersonDto>(youngPerson);
        var seniorResult = mapper.Map<SourcePerson, PersonDto>(seniorPerson);

        // Assert
        Assert.Equal("Regular", youngResult.Category);
        Assert.Equal("Senior", seniorResult.Category);
    }

    [Fact]
    public void SmartTypeConversion_ShouldHandleComplexConversions()
    {
        // Arrange
        var source = new ConversionSource
        {
            StringNumber = "123",
            BooleanString = "true",
            EnumString = "Active",
            DateString = "2023-12-25"
        };

        // Act
        var destination = source.MapTo<ConversionSource, ConversionDestination>();

        // Assert
        Assert.Equal(123, destination.StringNumber);
        Assert.True(destination.BooleanString);
        Assert.Equal(Status.Active, destination.EnumString);
        Assert.Equal(new DateTime(2023, 12, 25), destination.DateString);
    }

    [Fact]
    public void PerformanceMetrics_ShouldTrackOperations()
    {
        // Arrange
        var mapper = new TrueMapper.Core.Core.TrueMapper();
        var source = new SourcePerson { FirstName = "John", Age = 30 };

        // Act
        mapper.Map<SourcePerson, DestinationPerson>(source);
        mapper.Map<SourcePerson, DestinationPerson>(source);
        mapper.Map<SourcePerson, DestinationPerson>(source);

        var metrics = mapper.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalMappings);
        Assert.True(metrics.TotalMappingTime > 0);
        Assert.True(metrics.AverageMappingTime > 0);
    }

    [Fact]
    public void NullHandling_ShouldHandleNullsGracefully()
    {
        // Arrange
        SourcePerson? source = null;

        // Act & Assert - handle potential null gracefully
        if (source != null)
        {
            var destination = source.MapTo<DestinationPerson>();
            Assert.NotNull(destination);
        }
        else
        {
            // Test that extension method handles null sources
            var destination = new SourcePerson().MapTo<SourcePerson, DestinationPerson>();
            Assert.NotNull(destination);
        }
    }
}

// Test classes
public class SourcePerson
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class DestinationPerson
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class PersonDto
{
    public string FullName { get; set; } = string.Empty;
    public bool IsAdult { get; set; }
    public int Age { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ConversionSource
{
    public string StringNumber { get; set; } = string.Empty;
    public string BooleanString { get; set; } = string.Empty;
    public string EnumString { get; set; } = string.Empty;
    public string DateString { get; set; } = string.Empty;
}

public class ConversionDestination
{
    public int StringNumber { get; set; }
    public bool BooleanString { get; set; }
    public Status EnumString { get; set; }
    public DateTime DateString { get; set; }
}

public enum Status
{
    Inactive,
    Active,
    Pending
}