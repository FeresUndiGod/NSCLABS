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

        [Fact]
        public void Networking_Should_Not_Depend_On_System_Xml()
        {
            // Arrange
            var assembly = typeof(TcpClientWrapper).Assembly;
            // Act
            var result = Types.InAssembly(assembly)
                .That().ResideInNamespace("NetSdrClientApp.Networking")
                .Should().NotHaveDependencyOn("System.Xml")
                .GetResult();
            // Assert
            Assert.True(result.IsSuccessful, "Networking не повинен залежати від System.Xml");
        }
}