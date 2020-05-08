using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SplurgeStop.Data.EF;
using SplurgeStop.Data.EF.Repositories;
using SplurgeStop.Domain.PurchaseTransaction;
using SplurgeStop.UI.WebApi.Controllers;
using Xunit;
using static SplurgeStop.Integration.Tests.HelperMethods;

namespace SplurgeStop.Integration.Tests
{
    public sealed class DatabaseFixture : IDisposable
    {
        internal SplurgeStopDbContext context;
        internal string connectionString;

        public DatabaseFixture()
        {
            connectionString = ConnectivityService.GetConnectionString("TEMP");
            context = new SplurgeStopDbContext(connectionString);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            context.Database.ExecuteSqlRaw("DELETE FROM Purchases");
            context.Database.ExecuteSqlRaw("DELETE FROM Stores");
            context.Database.ExecuteSqlRaw("DELETE FROM LineItem");
        }
    }

    [Trait("Integration", "Database")]
    public sealed class DatabaseTests : IClassFixture<DatabaseFixture>
    {
        DatabaseFixture fixture;

        public DatabaseTests(DatabaseFixture fixture)
        {
            this.fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        [Fact]
        public async Task Purchase_transaction_inserted_to_database()
        {
            PurchaseTransactionId transactionId = await CreateValidPurchaseTransaction(fixture.context);

            var repository = new PurchaseTransactionRepository(fixture.context);
            Assert.True(await repository.Exists(transactionId));
        }

        [Fact]
        public async Task Purchase_transaction_already_exists()
        {
            PurchaseTransactionId transactionId = await CreateValidPurchaseTransaction(fixture.context);

            var repository = new PurchaseTransactionRepository(fixture.context);
            Assert.True(await repository.Exists(transactionId));

            var unitOfWork = new EfCoreUnitOfWork(fixture.context);
            var service = new PurchaseTransactionService(repository, unitOfWork);
            var transaction = new PurchaseTransaction();
            
            var transactionController = new PurchaseTransactionController(service);
            var command = new Commands.Create();
            command.Transaction = transaction;

            await transactionController.Post(command);

            await Assert.ThrowsAsync<InvalidOperationException>(async ()
                => await transactionController.Post(command));
        }

        [Fact]
        public async Task Update_transaction_date()
        {
            PurchaseTransactionId transactionId = await CreateValidPurchaseTransaction(fixture.context);

            var repository = new PurchaseTransactionRepository(fixture.context);
            Assert.True(await repository.Exists(transactionId));

            var sut = await repository.LoadAllAsync(transactionId);
            //await fixture.context.Entry(sut).ReloadAsync();

            Assert.Equal(DateTime.Now.Date, sut.PurchaseDate);

            await UpdatePurchaseDate(sut.Id, DateTime.Now.AddDays(-1), fixture.context);

            await fixture.context.Entry(sut).ReloadAsync();

            Assert.Equal(DateTime.Now.AddDays(-1).Date, sut.PurchaseDate);
        }

        [Fact]
        public async Task Invalid_Purchase_Transaction_Cannot_Be_Persisted_To_Database()
        {
            var repository = new PurchaseTransactionRepository(fixture.context);
            var unitOfWork = new EfCoreUnitOfWork(fixture.context);
            var service = new PurchaseTransactionService(repository, unitOfWork);
            var transaction = new PurchaseTransaction();
         
            var transactionController = new PurchaseTransactionController(service);
            var command = new Commands.Create();
            command.Transaction = transaction;

            await transactionController.Post(command);
            var sut = await fixture.context.Entry(transaction).GetDatabaseValuesAsync();

            Assert.Null(sut);
        }
    }
}
