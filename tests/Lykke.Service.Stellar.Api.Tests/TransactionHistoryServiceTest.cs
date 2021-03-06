﻿using Castle.Components.DictionaryAdapter;
using Common.Log;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Services.Transaction;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.responses;
using Xunit;
using Asset = Lykke.Service.Stellar.Api.Core.Domain.Asset;

namespace Lykke.Service.Stellar.Api.Tests
{
    public class TransactionHistoryServiceTest
    {
        [Fact]
        public async Task TransactionHistory_UpdateWallets_SkipErrorMemo()
        {
            Mock<IBlockchainAssetsService> blockchainAssetsService = new Mock<IBlockchainAssetsService>();
            Mock<IBalanceService> balanceService = new Mock<IBalanceService>();
            Mock<ITxHistoryRepository> txHistoryRepository = new Mock<ITxHistoryRepository>();
            Mock<IHorizonService> horizonService = new Mock<IHorizonService>();
            Mock<IObservationRepository<TransactionHistoryObservation>> observationRepository 
                = new Mock<IObservationRepository<TransactionHistoryObservation>>();
            Mock<IKeyValueStoreRepository> keyValueStoreRepository = new Mock<IKeyValueStoreRepository>();
            Mock<IWalletBalanceRepository> walletBalanceRepository = new Mock<IWalletBalanceRepository>();
            string depositBaseAddress = "GCCWY6MNVWORHO7B3L6W5LULGFR337K5UAATA2Z3FSPQA5MYRW5X33ZP";
            string memo = "http://stellar-win.me/";
            string[] explorerUrlFormats = new []{""};
            Mock<ILog> l1 = new Mock<ILog>();
            Mock<ILogFactory> log = new Mock<ILogFactory>();
            log.Setup(x => x.CreateLog(It.IsAny<object>())).Returns(l1.Object);
            

            horizonService.Setup(x => x.GetMemo(It.IsAny<TransactionResponse>())).Returns(memo);
            balanceService.Setup(x => x.GetDepositBaseAddress()).Returns(depositBaseAddress);
            txHistoryRepository.Setup(x => x.InsertOrReplaceAsync(It.IsAny<TxDirectionType>(), It.IsAny<TxHistory>()))
                .Returns(Task.FromResult(0)).Verifiable();
            horizonService.Setup(x => x.GetTransactions(depositBaseAddress,
                OrderDirection.ASC,
                null,
                It.IsAny<int>()))
                .ReturnsAsync(new EditableList<TransactionResponse>()
                {
                    new TransactionResponse("hash", 1, DateTime.UtcNow.ToString(), "", true, "", 1, 1000, 1,
                        "AAAAAM2Tf7J/6Vt6MRtYUwilF0fY5ndYLYtRWBHT6QU6LO5VAAAAZAByK8oAAAAlAAAAAAAAAAAAAAABAAAAAQAAAADNk3+yf+lbejEbWFMIpRdH2OZ3WC2LUVgR0+kFOizuVQAAAAEAAAAAhWx5ja2dE7vh2v1urosxY739XaABMGs7LJ8AdZiNu30AAAAAAAAAAAABhqEAAAAAAAAAATos7lUAAABAbYa73/EQqpadqoXuVhQyzcjf0jWOAAqsIiRmi0Qlmqbh8EyrhIlkPYgiteg2tTGujhiGN9EhNodGCIKyfmvjAA==",
                        "", "", Memo.Text(memo), null)
                });

            blockchainAssetsService.Setup(x => x.GetNativeAsset())
                .Returns(new Asset("XLM", "", "Stellar Lumen", "native", 7));

            TransactionHistoryService transactionHistoryService = new TransactionHistoryService(balanceService.Object,
                horizonService.Object,
                keyValueStoreRepository.Object,
                observationRepository.Object,
                txHistoryRepository.Object,
                log.Object,
                blockchainAssetsService.Object);



            await transactionHistoryService.UpdateDepositBaseTransactionHistory();
            balanceService.Setup(x => x.GetDepositBaseAddress()).Returns(depositBaseAddress);
            txHistoryRepository
                .Verify(x => x.InsertOrReplaceAsync(It.IsAny<TxDirectionType>(), It.IsAny<TxHistory>()), Times.Never());
        }

        [Fact]
        public async Task TransactionHistory_UpdateWallets_ProcessDepositMemo()
        {
            Mock<IBlockchainAssetsService> blockchainAssetsService = new Mock<IBlockchainAssetsService>();
            Mock<IBalanceService> balanceService = new Mock<IBalanceService>();
            Mock<ITxHistoryRepository> txHistoryRepository = new Mock<ITxHistoryRepository>();
            Mock<IHorizonService> horizonService = new Mock<IHorizonService>();
            Mock<IObservationRepository<TransactionHistoryObservation>> observationRepository
                = new Mock<IObservationRepository<TransactionHistoryObservation>>();
            Mock<IKeyValueStoreRepository> keyValueStoreRepository = new Mock<IKeyValueStoreRepository>();
            Mock<IWalletBalanceRepository> walletBalanceRepository = new Mock<IWalletBalanceRepository>();
            string depositBaseAddress = "GCCWY6MNVWORHO7B3L6W5LULGFR337K5UAATA2Z3FSPQA5MYRW5X33ZP";
            string memo = "r6mzsfwnbkgwtc8cktx4i5nw8e";
            string[] explorerUrlFormats = new[] { "" };
            Mock<ILog> l1 = new Mock<ILog>();
            Mock<ILogFactory> log = new Mock<ILogFactory>();
            log.Setup(x => x.CreateLog(It.IsAny<object>())).Returns(l1.Object);

            horizonService.Setup(x => x.GetMemo(It.IsAny<TransactionResponse>())).Returns(memo);
            balanceService.Setup(x => x.GetDepositBaseAddress()).Returns(depositBaseAddress);
            txHistoryRepository.Setup(x => x.InsertOrReplaceAsync(It.IsAny<TxDirectionType>(), It.IsAny<TxHistory>()))
                .Returns(Task.FromResult(0)).Verifiable();
            horizonService.Setup(x => x.GetTransactions(depositBaseAddress,
                OrderDirection.ASC,
                null,
                It.IsAny<int>()))
                .ReturnsAsync(new EditableList<TransactionResponse>()
                {
                    new TransactionResponse("hash", 1, DateTime.UtcNow.ToString(), "", true, "", 1, 1000, 1,
                        "AAAAAM2Tf7J/6Vt6MRtYUwilF0fY5ndYLYtRWBHT6QU6LO5VAAAAZAByK8oAAAAlAAAAAAAAAAAAAAABAAAAAQAAAADNk3+yf+lbejEbWFMIpRdH2OZ3WC2LUVgR0+kFOizuVQAAAAEAAAAAhWx5ja2dE7vh2v1urosxY739XaABMGs7LJ8AdZiNu30AAAAAAAAAAAABhqEAAAAAAAAAATos7lUAAABAbYa73/EQqpadqoXuVhQyzcjf0jWOAAqsIiRmi0Qlmqbh8EyrhIlkPYgiteg2tTGujhiGN9EhNodGCIKyfmvjAA==",
                        "", "", Memo.Text(memo), null)
                });

            blockchainAssetsService.Setup(x => x.GetNativeAsset())
                .Returns(new Asset("XLM", "", "Stellar Lumen", "native", 7));

            TransactionHistoryService transactionHistoryService = new TransactionHistoryService(balanceService.Object,
                horizonService.Object,
                keyValueStoreRepository.Object,
                observationRepository.Object,
                txHistoryRepository.Object,
                log.Object,
                blockchainAssetsService.Object);



            await transactionHistoryService.UpdateDepositBaseTransactionHistory();
            balanceService.Setup(x => x.GetDepositBaseAddress()).Returns(depositBaseAddress);
            txHistoryRepository
                .Verify(x => x.InsertOrReplaceAsync(It.IsAny<TxDirectionType>(), It.IsAny<TxHistory>()), Times.AtLeastOnce);
        }
    }
}
