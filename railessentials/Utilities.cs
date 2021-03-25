// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Utilities.cs

using System.Data;
using railessentials.Plan;
using Utilities;

namespace railessentials
{
    public static class Utilities
    {
        public static void GetEcosAddress(
            PlanField field,
            int coordX, int coordY,
            out int? ecosAddr1,
            out int? ecosAddr2,
            out bool ecosAddr1Inverse,
            out bool ecosAddr2Inverse)
        {
            var item = field.Get(coordX, coordY);

            GetFeedbackAddress(item, out ecosAddr1, out ecosAddr2, out ecosAddr1Inverse, out ecosAddr2Inverse);
        }

        public static bool GetFeedbackAddress(
            PlanItem item,
            out int? ecosAddr1,
            out int? ecosAddr2,
            out bool ecosAddr1Inverse,
            out bool ecosAddr2Inverse)
        {
            ecosAddr1Inverse = false;
            ecosAddr2Inverse = false;

            ecosAddr1 = null;
            ecosAddr2 = null;

            // {
            //  "Addr": 0,
            //  "Addr1": 17, "Port1": 3, "Inverse1": false,
            //  "Addr2":  0, "Port2": 0, "Inverse2": false
            // }
            if (item?.Addresses != null && item.Addresses.Port1 > 0 && item.Addresses.Addr1 > 0)
            {
                ecosAddr1 = AddressUtilities.GetEcosAddress(item.Addresses.Addr1, item.Addresses.Port1);
                ecosAddr1Inverse = item.Addresses.Inverse1;
            }

            if (item?.Addresses != null && item.Addresses.Port2 > 0 && item.Addresses.Addr2 > 0)
            {
                ecosAddr2 = AddressUtilities.GetEcosAddress(item.Addresses.Addr2, item.Addresses.Port2);
                ecosAddr2Inverse = item.Addresses.Inverse2;
            }

            return ecosAddr1 != null && ecosAddr2 != null;
        }

        public static void GetValidAddress(
            int ecosAddr1, int ecosAddr2, 
            bool ecosAddr1Inverse, bool ecosAddr2Inverse,
            out int ecosAddr, out bool ecosAddrInverse)
        {
            if(ecosAddr1 > 0)
            {
                ecosAddr = ecosAddr1;
                ecosAddrInverse = ecosAddr1Inverse;
            }
            else if(ecosAddr2 > 0)
            {
                ecosAddr = ecosAddr2;
                ecosAddrInverse = ecosAddr2Inverse;
            }
            else
            {
                ecosAddr = 0;
                ecosAddrInverse = false;
            }
        }

        public static void GetAccessoryEcosAddresses(
            PlanItem item, 
            out int addr1,
            out int addr2)
        {
            GetFeedbackAddress(item, out var ecosAddr1, out var ecosAddr2, out _, out _);
            if(ecosAddr1 == null && ecosAddr2 == null)
            {
                addr1 = item.Addresses.Addr;
                addr2 = 0;
            }
            else if(ecosAddr1 != null)
            {
                addr1 = ecosAddr1.Value;
                addr2 = 0;
            }
            else
            {
                addr1 = 0;
                addr2 = ecosAddr2.Value;
            }
        }
    }
}
