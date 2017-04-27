makecert -r -pe -n "CN=OICD2 Server" -b 01/01/2017 -e 01/01/2027 -sky exchange Server.cer -sv Server.pvk
pvk2pfx.exe -pvk Server.pvk -spc Server.cer -pfx Server.pfx    