using NetArchTest.Rules;
using Xunit;
using NetSdrClientApp.Networking; // Підключи свій namespace
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // ТЕСТ 1: Перевірка неймінгу (Інтерфейси мають починатися з "I")
        [Fact]
        public void Interfaces_Should_Start_With_I()
        {
            // Arrange
            var assembly = typeof(TcpClientWrapper).Assembly;

            // Act
            var result = Types.InAssembly(assembly)
                .That().AreInterfaces()
                .Should().HaveNameStartingWith("I")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Усі інтерфейси мають починатися з літери 'I'");
        }

        // ТЕСТ 2 (Для провалу): Networking не повинен знати про System.Net.Sockets
        [Fact]
        public void Networking_Should_Not_Depend_On_Sockets_Directly()
        {
            // Arrange
            var assembly = typeof(TcpClientWrapper).Assembly;

            // Act
            // Ми кажемо: "Класи в NetSdrClientApp.Networking НЕ повинні використовувати System.Net.Sockets"
            var result = Types.InAssembly(assembly)
                .That().ResideInNamespace("NetSdrClientApp.Networking")
                .Should().NotHaveDependencyOn("System.Net.Sockets")
                .GetResult();

            // Assert
            Assert.True(result.IsSuccessful, "Архітектурна помилка: Networking не повинен залежати від Sockets!");
        }
    }
}