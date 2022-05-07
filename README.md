# KeyFile-POC
This is a simple WPF application written in C# as part of my Masters program capstone project. 
It allows users to view and modify the EFS encryption properties of local files. 
The system allows for the user to query public DNS for CERT records and add the returned public key as an authorized user of the selected file. 

****This is only a POC and there are several potential vulnerabilities and bugs within the code. Please do not use this code for any 
real-life purposes without thoroughly reviewing its security.***

System Requirements:
- Windows 7/8/10/11 Professional (must have NTFS Encrypting File System)
   -cipher.exe (should be installed already) 
- You must also install ISC 'dig' tools for DNS lookups (https://downloads.isc.org/isc/bind9/9.16.28/BIND9.16.28.x64.zip) 
- This program queries DNS for 'CERT' resource records. Many DNS providers do not support adding CERT records, I used CloudFlare as a test for this POC.
