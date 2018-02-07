﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase = Stellar;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services
{
    public class BalanceService : IBalanceService
    {
        private const int BatchSize = 100;

        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BalanceObservation> _observationRepository;
        private readonly IWalletBalanceRepository _walletBalanceRepository;

        public BalanceService(IHorizonService horizonService, IObservationRepository<BalanceObservation> observationRepository, IWalletBalanceRepository walletBalanceRepository)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _walletBalanceRepository = walletBalanceRepository;
        }

        public bool IsAddressValid(string address)
        {
            try
            {
                StellarBase.StrKey.DecodeCheck(StellarBase.VersionByte.ed25519Publickey, address);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null)
        {
            var result = new AddressBalance
            {
                Address = address
            };

            var accountDetails = await _horizonService.GetAccountDetails(address);
            result.Sequence = Int64.Parse(accountDetails.Sequence);

            var nativeBalance = accountDetails.Balances.Single(b => "native".Equals(b.AssetType, StringComparison.OrdinalIgnoreCase));
            result.Balance = Convert.ToInt64(Decimal.Parse(nativeBalance.Balance) * StellarBase.One.Value);
            if (fees != null)
            {
                long entries = accountDetails.Signers.Length + accountDetails.SubentryCount;
                var minBalance = (2 + entries) * fees.BaseReserve * StellarBase.One.Value;
                result.MinBalance = Convert.ToInt64(minBalance);
            }

            return result;
        }

        public async Task<bool> IsBalanceObservedAsync(string address)
        {
            return await _observationRepository.GetAsync(address) != null;
        }

        public async Task AddBalanceObservationAsync(string address)
        {
            var observation = new BalanceObservation
            {
                Address = address
            };
            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task DeleteBalanceObservationAsync(string address)
        {
            await _observationRepository.DeleteAsync(address);
            await _walletBalanceRepository.DeleteIfExistAsync(address);
        }

        public async Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken)
        {
            return await _walletBalanceRepository.GetAllAsync(take, continuationToken);
        }

        private async Task ProcessWallet(string address)
        {
            var addressBalance = await GetAddressBalanceAsync(address);
            if (addressBalance.Balance > 0)
            {
                var walletEntry = await _walletBalanceRepository.GetAsync(address);
                if (walletEntry == null)
                {
                    walletEntry = new WalletBalance
                    {
                        Address = address
                    };
                }
                if (walletEntry.Balance != addressBalance.Balance)
                {
                    walletEntry.Balance = addressBalance.Balance;
                    // TODO: find ledger of last payment
                    await _walletBalanceRepository.InsertOrReplaceAsync(walletEntry);
                }
            }
            else
            {
                await _walletBalanceRepository.DeleteIfExistAsync(address);
            }
        }

        public async Task UpdateWalletBalances()
        {
            string continuationToken = null;
            do
            {
                var observations = await _observationRepository.GetAllAsync(BatchSize, continuationToken);
                foreach (var item in observations.Items)
                {
                    await ProcessWallet(item.Address);
                }
                continuationToken = observations.ContinuationToken;
            } while (continuationToken != null);
        }
    }
}
