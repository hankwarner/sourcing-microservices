using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq.AutoMock;
using Xunit;

namespace ServiceSourcing.UnitTest
{
    public class TestDemoControllers
    {
        private readonly AutoMocker _mocker;

        public TestDemoControllers()
        {
            _mocker = new AutoMocker();
        }

        [Fact]
        public async Task Test_OldController()
        {
            var controller = _mocker.CreateInstance<OldCustomerController>();
            var response   = await controller.Create();
            var returnedData = response.Value.ToList();

            Assert.NotNull(response.Value);
            Assert.NotEmpty(returnedData);
            Assert.True(returnedData.All(s => !string.IsNullOrEmpty(s)));
        }

        public static IEnumerable<object[]> TestData =>
            new List<object[]>
            {
                new object[] {"GA", true},
                new object[] {"NonExistantRegionCode", false},
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task Test_NewController(string regionCode, bool shouldReturnValue)
        {
            var controller = _mocker.CreateInstance<NewCustomerController>();
            var response = await controller.Create(regionCode);

            if (shouldReturnValue)
            {
                Assert.NotNull(response.Value);
            }
            else
            {
                Assert.IsType<NotFoundResult>(response.Result);
                Assert.Null(response.Value);
            }
        }
    }
}