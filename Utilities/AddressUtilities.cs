// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AddressUtilities.cs

namespace Utilities
{
    public static class AddressUtilities
    {
        public static int GetEcosAddress(int dccAddr, int dccPort)
        {
            return (dccAddr - 1) * 4 + dccPort;
        }

        public static int GetDccAddr(int ecosAddr)
        {
            if (ecosAddr < 0) return -1;
            // Address = (ECoS-Address -1) / 4 + 1
            return (ecosAddr - 1) / 4 + 1;
        }

        public static int GetDccPort(int ecosAddr)
        {
            if (ecosAddr < 0) return -1;
            // Port = (ECoS-Address -1) mod 4 + 1
            return (ecosAddr - 1) % 4 + 1;
        }
    }
}
