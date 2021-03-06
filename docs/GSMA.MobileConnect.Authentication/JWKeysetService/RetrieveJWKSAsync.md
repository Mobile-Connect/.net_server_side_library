JWKeysetService.RetrieveJWKSAsync Method
========================================
Retrieve the JSON Web Keyset from the specified url utilising caching if configured

**Namespace:** [GSMA.MobileConnect.Authentication][1]  
**Assembly:** GSMA.MobileConnect (in GSMA.MobileConnect.dll)

Syntax
------

```csharp
public Task<JWKeyset> RetrieveJWKSAsync(
	string url
)
```

#### Parameters

##### *url*
Type: [System.String][2]  
JWKS URL

#### Return Value
Type: [Task][3]&lt;[JWKeyset][4]>  
JSON Web Keyset if successfully retrieved
#### Implements
[IJWKeysetService.RetrieveJWKSAsync(String)][5]  


See Also
--------

#### Reference
[JWKeysetService Class][6]  
[GSMA.MobileConnect.Authentication Namespace][1]  

[1]: ../README.md
[2]: http://msdn.microsoft.com/en-us/library/s1wwdcbf
[3]: http://msdn.microsoft.com/en-us/library/dd321424
[4]: ../JWKeyset/README.md
[5]: ../IJWKeysetService/RetrieveJWKSAsync.md
[6]: README.md
[7]: ../../_icons/Help.png