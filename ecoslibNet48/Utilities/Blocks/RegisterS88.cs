// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RegisterS88.cs

using System;
using ecoslib.Utilities.Replies;

namespace ecoslib.Utilities.Blocks
{
    public class RegisterS88
    {
        public class S88Info
        {
            public int ObjectId { get; set; }
            public bool Appended { get; set; }
            public int NewSize { get; set; }
        }

        /*
         * S88-Device wird angemeldet:
         * ---------------------------
         * <EVENT 26>
         * 26 msg[LIST_CHANGED]
         * 104 appended
         * 26 size[5]
         * <END 0 (OK)>

         * S88-Device wird abgemeldet:
         * ---------------------------
         * <EVENT 26>
         * 26 msg[LIST_CHANGED]
         * 104 removed
         * 26 size[4]
         * <END 0 (OK)>
         */

        public static bool IsRegisterS88(IBlock block, out S88Info s88Info)
        {
            s88Info = null;

            var evBlock = block as EventBlock;
            if (evBlock == null)
                return false;

            if (evBlock.ObjectId != Globals.ID_EV_S88)
                return false;

            var ncmdlines = evBlock.ListEntries.Count;
            if (ncmdlines != 3)
                return false;

            var cmd0 = evBlock.ListEntries[0];
            var cmd1 = evBlock.ListEntries[1];
            var cmd2 = evBlock.ListEntries[2];

            if (cmd0.ObjectId != Globals.ID_EV_S88)
                return false;

            if (cmd2.ObjectId != Globals.ID_EV_S88)
                return false;

            if (cmd1.Arguments.Count != 1)
                return false;

            var arg10 = cmd1.Arguments[0] as ICommandArgument;
            var arg20 = cmd2.Arguments[0] as ICommandArgument;

            if (arg10 == null) return false;
            if (arg20 == null) return false;

            var appendedS88 = arg10.Name.Equals("appended", StringComparison.OrdinalIgnoreCase);

            int newSize;
            if (!int.TryParse(arg20.Parameter[0], out newSize))
                return false;

            s88Info = new S88Info()
            {
                ObjectId = cmd1.ObjectId,
                Appended = appendedS88,
                NewSize = newSize
            };

            return true;
        }
    }
}
