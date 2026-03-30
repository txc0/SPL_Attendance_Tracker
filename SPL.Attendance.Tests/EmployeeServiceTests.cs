using FluentAssertions;
using Moq;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Business.Services;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;
using Xunit;

namespace SPL.Attendance.Tests
{
    /// <summary>
    /// Unit tests for EmployeeService — Create, Update, Deactivate, and validation rules.
    /// </summary>
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _repoMock;
        private readonly EmployeeService _sut;

        public EmployeeServiceTests()
        {
            _repoMock = new Mock<IEmployeeRepository>();
            _sut = new EmployeeService(_repoMock.Object);
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE — happy path
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateEmployee_ValidInput_ShouldSucceedAndReturnDto()
        {
            // Arrange
            var dto = new CreateEmployeeDto
            {
                EmployeeCode = "EMP099",
                Name         = "Test User",
                Email        = "test@spl.com",
                SupervisorId = null
            };

            _repoMock.Setup(r => r.CodeExistsAsync("EMP099", null)).ReturnsAsync(false);

            var savedEmployee = new Employee
            {
                Id = 10, EmployeeCode = "EMP099", Name = "Test User",
                Email = "test@spl.com", IsActive = true
            };

            _repoMock.Setup(r => r.AddAsync(It.IsAny<Employee>()))
                     .ReturnsAsync(savedEmployee);

            _repoMock.Setup(r => r.GetByIdAsync(10))
                     .ReturnsAsync(savedEmployee);

            // Act
            var result = await _sut.CreateEmployeeAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.EmployeeCode.Should().Be("EMP099");
            result.Name.Should().Be("Test User");
            result.IsActive.Should().BeTrue();
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE — duplicate employee code
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateEmployee_DuplicateCode_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _repoMock.Setup(r => r.CodeExistsAsync("EMP001", null)).ReturnsAsync(true);

            var dto = new CreateEmployeeDto { EmployeeCode = "EMP001", Name = "Duplicate" };

            // Act
            var act = async () => await _sut.CreateEmployeeAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already in use*");
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE — supervisor not found
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateEmployee_InvalidSupervisor_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _repoMock.Setup(r => r.CodeExistsAsync("EMP050", null)).ReturnsAsync(false);
            _repoMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            var dto = new CreateEmployeeDto
            {
                EmployeeCode = "EMP050",
                Name         = "New Guy",
                SupervisorId = 999 // does not exist
            };

            // Act
            var act = async () => await _sut.CreateEmployeeAsync(dto);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Supervisor*not found*");
        }

        // ════════════════════════════════════════════════════════════════════
        // UPDATE — self-supervisor guard
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateEmployee_SelfAsSupervisor_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existing = new Employee { Id = 5, Name = "Alice", EmployeeCode = "EMP005", IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            var dto = new UpdateEmployeeDto { Name = "Alice", SupervisorId = 5 }; // self

            // Act
            var act = async () => await _sut.UpdateEmployeeAsync(5, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*cannot be their own supervisor*");
        }

        // ════════════════════════════════════════════════════════════════════
        // DEACTIVATE — employee not found
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeactivateEmployee_NotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _repoMock.Setup(r => r.ExistsAsync(9999)).ReturnsAsync(false);

            // Act
            var act = async () => await _sut.DeactivateEmployeeAsync(9999);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ════════════════════════════════════════════════════════════════════
        // DEACTIVATE — happy path
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeactivateEmployee_ValidId_ShouldCallDeactivateOnce()
        {
            // Arrange
            _repoMock.Setup(r => r.ExistsAsync(3)).ReturnsAsync(true);
            _repoMock.Setup(r => r.DeactivateAsync(3)).ReturnsAsync(true);

            // Act
            await _sut.DeactivateEmployeeAsync(3);

            // Assert
            _repoMock.Verify(r => r.DeactivateAsync(3), Times.Once);
        }
    }
}
