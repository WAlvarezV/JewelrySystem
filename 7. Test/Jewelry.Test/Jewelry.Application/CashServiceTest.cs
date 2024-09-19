using AutoFixture;
using AutoMapper;
using NLog;
using NSubstitute;
using Pomona.Infrastructure.Implementation;

namespace Jewelry.Test.Jewelry.Application
{
    public sealed class CashServiceTest
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private static readonly Logger _logger;
        private readonly IFixture _fixture;

        public CashServiceTest()
        {
            _uow = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();

            _fixture = new Fixture();
        }

        [Fact]
        public void RegisterDailyRecord_Should_Async()
        {
            //Arrange

            //Act

            //Assert
        }
    }
}
