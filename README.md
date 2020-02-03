# Alternative Software for Top2005+ Universal Programmer #
----
Please read the [wiki](https://github.com/elgendk/u2pa/wiki) for a detailed description of the programme.

----
# Latest news: #

----
*At present u2pa only supports the Top programmer Top2005+.
As this is the only Top programmer I own, I can't implement support for any others.
Should anyone however, wish to donate me a different Top programmer (new? used? doesn't matter as long as it works), I'll try to do an effort to implement support for it.*

----
* Reading of a lot of bipolar PROMs implemented in xml:
    * These are read without further ado:
        * `N82S131 (AM27S13, 93466)`
        * `N82S129 (TBP24S10, MMI6301, MMI63s141, IM5623, N82S126, 7610)`
        * `N82S123 (TBP18S030, MMI6331, IM5610, N82S23)`
        * `N82S195 (HM1-76165-5)`
        * `N82S181 (MB7132, HM1-7631-5)`
        * `N82S141 (MMI-6341-1J)`
        * `N82S191 (AM27S191)`
    * These requires a single jumper wire for GND connection as they are dil18:
        * `N82S185 (HM1-7685-5)`
        * `N82S137 (MB7122, 63S441, HM1-7643-5)`    
* An alias system for roms
* Support for defining adaptors in XML
* A simple vector test
* The ability to generate CUPL-equations from binary dumps of a small roms

I will not do "releases" as such, but you can get the "bleeding edge code" by checking out the tip of the repo, or by using the "Clone or download" function.
