﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class Emulator
    {
        public void Load( byte[] rom )
        {
            byte[] gameNameBytes = new byte[16];

            Array.Copy(rom, 0x134, gameNameBytes, 0, 16);

            GameName = System.Text.Encoding.ASCII.GetString( gameNameBytes );
        }

        public string GameName { get; private set; }
    }
}
