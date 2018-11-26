﻿using System;
using System.Globalization;

namespace Lykke.Service.Stellar.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id,
                     string address,
                     string name,
                     string typeName,
                     int accuracy)
            => (Id, Address, Name, TypeName, Accuracy) = (id, address, name, typeName, accuracy);

        public string Id { get; }
        public string Address { get; }
        public string Name { get; }
        public string TypeName { get; }
        public int Accuracy { get; }

        public long ParseDecimal(string value)
        {
            var pow = Convert.ToDecimal(Math.Pow(10, Accuracy));
            var dec = decimal.Parse(value, CultureInfo.InvariantCulture);
            var mul = decimal.Round(dec * pow);
            var res = Convert.ToInt64(mul);

            return res;
        }
    }
}
