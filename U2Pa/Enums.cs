using System;

namespace U2Pa
{
  public enum VppLevel : byte
  {
    /// <summary>
    /// Boost off.
    /// <remarks>
    /// Voltage will be around 4.6v because the boost has a schottky diode from 5v to VPP.
    /// Can never truly go to 0v.
    /// </remarks>
    /// </summary>
    Vpp_Off = 0x00,
    Vpp_9_37v = 0x33,
    Vpp_9_95v = 0x5f,
    Vpp_10_44v = 0x64,
    Vpp_11_03v = 0x69,
    Vpp_12_00v = 0x78,
    Vpp_12_61v = 0x7d,
    Vpp_13_10v = 0x82,
    Vpp_13_70v = 0x87,
    Vpp_14_79v = 0x91,
    Vpp_16_36v = 0x9b,
    Vpp_21_11v = 0xd2,
    Vpp_25_59v = 0xfa,
  }

  public enum VccLevel : byte
  {
    Vcc_2_5v = 0x19,
    Vcc_3_3v = 0x21,
    Vcc_5_0v = 0x32,
  }
}