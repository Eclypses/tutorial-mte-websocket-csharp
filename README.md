<img src="./Eclypses.png" style="width:50%;margin-right:0;"/>

<div align="center" style="font-size:40pt; font-weight:900; font-family:arial; margin-top:300px; " >
C# WebSocket Tutorial</div>



<div align="center" style="font-size:15pt; font-family:arial; " >
Using MTE version 3.1.x</div>

<div style="page-break-after: always; break-after: page;"></div>

# Introduction

This tutorial is sending messages via a WebSocket connection. This is only a sample, the MTE does NOT require the usage of WebSockets, you can use whatever communication protocol that is needed.

This tutorial demonstrates how to use MTE Core, MTE MKE and MTE Fixed Length. Depending on what your needs are, these three different implementations can be used in the same application OR you can use any one of them. They are not dependent on each other and can run simultaneously in the same application if needed.

The SDK that you received from Eclypses may not include the MKE or MTE FLEN add-ons. If your SDK contains either the MKE or the Fixed Length add-ons, the name of the SDK will contain "-MKE" or "-FLEN". If these add-ons are not there and you need them please work with your sales associate. If there is no need, please just ignore the MKE and FLEN options.

Here is a short explanation of when to use each, but it is encouraged to either speak to a sales associate or read the dev guide if you have additional concerns or questions.

***MTE Core:*** This is the recommended version of the MTE to use. Unless payloads are large or sequencing is needed this is the recommended version of the MTE and the most secure.

***MTE MKE:*** This version of the MTE is recommended when payloads are very large, the MTE Core would, depending on the token byte size, be multiple times larger than the original payload. Because this uses the MTE technology on encryption keys and encrypts the payload, the payload is only enlarged minimally.

***MTE Fixed Length:*** This version of the MTE is very secure and is used when the resulting payload is desired to be the same size for every transmission. The Fixed Length add-on is mainly used when using the sequencing verifier with MTE. In order to skip dropped packets or handle asynchronous packets the sequencing verifier requires that all packets be a predictable size. If you do not wish to handle this with your application then the Fixed Length add-on is a great choice. This is ONLY an Encoder change - the Decoder that is used is the MTE Core Decoder.

In this tutorial we are creating an MTE Encoder and a MTE Decoder in the server as well as the client because we are sending messages in both directions. This is only needed when there are messages being sent from both sides.  If only one side of your application is sending messages, then the side that sends the messages should have an MTE Encoder and the side receiving the messages needs only a MTE Decoder.

These steps should be followed on the server side as well as on the client side of the program.

**IMPORTANT**

Please note the solution provided in this tutorial does NOT include the MTE library or any supporting MTE library files. If you have NOT been provided a MTE library and supporting files, please contact Eclypses Inc. Solution will only work AFTER MTE library and MTE library files have been included.

# Tutorial Overview

The structure of this tutorial is as follows:

```bash
.
├── finish
│   ├── client
│   └── server
└── start
    ├── client
    └── server
```

| Directory | Description                                                  |
| --------- | ------------------------------------------------------------ |
| `finish`  | Example of project after completing the tutorial to reference |
| `start`   | Project where you can follow along with the tutorial to implement the MTE |

*There is a Server and Client version of each project so that you can get it to talk to the same language.  However, you can grab either the server or client and pair it with an Eclypses tutorial of a different language that uses the same communication protocol.  Both the server and client are implemented in almost identical ways, so this will be an agnostic tutorial that can be followed both on the client and the server.*


# MTE Implementation

1. **Add MTE to your project**

   - Add the files in the `src/lib/cs/` directory of the MTE files you were provided into the root project; preferably in a new directory such as `include/`

   - Add the `mte.dll` (Windows), `libmte.so` (Linux), or `libmte.dylib` (Mac) file to the root of your project directory

     - The file that you will be adding here solely depends on the operating system running the application

     - The operating systems listed above are just the assumed system, we have many other libraries built for different hardware.  If you do not see what you're looking for, please contact Eclypses Inc.

     - Locate your `[project name].csproj` file and add the MTE library file to it.  Make sure it is copied to the output directory when the project is built.  A `.csproj` example file is provided below for a Windows OS (`mte.dll`):

       ```c#
       <Project Sdk="Microsoft.NET.Sdk">
       
         <PropertyGroup>
           <OutputType>Exe</OutputType>
           <TargetFramework>netcoreapp3.1</TargetFramework>
         </PropertyGroup>
       
         <ItemGroup>
           <None Update="mte.dll">
             <CopyToOutputDirectory>Always</CopyToOutputDirectory>
           </None>
         </ItemGroup>
       
       </Project>
       ```



2. **Navigate to the `Program.cs` (client) or `Startup.cs` (server) and add a `using Eclypses.MTE;`**



3. **Create the MTE Base, MTE Decoder, MTE Encoder, as well as the accompanying MTE status for each as global variables.**

   > If using the fixed length MTE (FLEN), all messages that are sent that are longer than the set fixed length will be trimmed by the MTE. The other side of the MTE will NOT contain the trimmed portion. Also messages that are shorter than the fixed length will be padded by the MTE so each message that is sent will ALWAYS be the same length. When shorter message are "decoded" on the other side the MTE takes off the extra padding when using strings and hands back the original shorter message, BUT if you use the raw interface the padding will be present as all zeros. Please see official MTE Documentation for more information.

```c#
private static MteBase _mteBase = new MteBase();

//---------------------------------------------------
// Comment out to use MKE or MTE FLEN instead of MTE Core
//---------------------------------------------------
private static MteDec _decoder = new MteDec();
private static MteEnc _encoder = new MteEnc();

//---------------------------------------------------
// Uncomment to use MKE instead of MTE Core
//---------------------------------------------------
// private static MteMkeDec _decoder = new MteMkeDec();
// private static MteMkeEnc _encoder = new MteMkeEnc();

//---------------------------------------------------
// Uncomment to use MTE FLEN Encoder instead of MTE Core Encoder
//---------------------------------------------------
// private const int _fixedLength = 8;
// private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
// private static MteDec _decoder = new MteDec();

private static MteStatus _encoderStatus = MteStatus.mte_status_success;
private static MteStatus _decoderStatus = MteStatus.mte_status_success;
```



5. **Next, we need to be able to get the entropy, nonce, and ientifier**

   - These values should be treated like encryption keys and never exposed. These values should also match with the device they are communicating with.  For demonstration purposes in the tutorial we are setting these values in the code. In a production environment these values should be protected and not available to outside sources.
   - We are adding 1 to the Decoder nonce (server side) so that the return value changes.  This is optional, the same nonce can be used for the Encoder and Decoder.  Client side values will be switched so they match up to the Encoder/Decoder and vice versa.

    ```csharp
   private static string _encoderEntropy = "";
   private static string _decoderEntropy = "";
   private static ulong _encoderNonce = 0;
   private static ulong _decoderNonce = 1;
   private static string _identifier = "demo";
    ```

   - To set the entropy in the tutorial we are getting the minimum bytes required and creating a string of that length that contains all zeros.

   - You will need an instance of the Encoder or Decoder to get the correct entropy based on the DRBG that they are using with the helper method `GetDrbg()`

     ```c#
     int entropyMinBytes = _mteBase.GetDrbgsEntropyMinBytes(_encoder.GetDrbg());
     _encoderEntropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _encoderEntropy;
     ```

     *If you are using a trial  version of the MTE, the entropy must be left as an empty string.*



6. **To ensure the MTE library is licensed correctly run the license check**

   - The `licenseCompanyName`, and `licenseKey` below should be replaced with your company’s MTE license information provided by Eclypses. If a trial version of the MTE is being used, any value can be passed into those fields.

   ```c#
   // Check and initialize MTE license
   if (!MteBase.InitLicense("licenseCompany", "licenseKey"))
   {
   		encoderStatus = MteStatus.mte_status_license_error;
     	Console.Error.WriteLine($"License error ({MteBase.GetStatusName(_encoderStatus)}): {MteBase.GetStatusDescription(_encoderStatus)}.  Press any key to end.");
     	Console.ReadLine();
     	return;
   }
   ```



7. **Create MTE Decoder Instance and MTE Encoder Instances in a couple functions.**

   Here is a sample function that creates the MTE Decoder.

   ```c#
   public static void InstantiateDecoder()
   {
     _decoder.SetEntropy(Encoding.UTF8.GetBytes(_decoderEntropy));
     _decoder.SetNonce(_decoderNonce);

  _decoderStatus = _decoder.Instantiate(_identifier);

     if (_decoderStatus != MteStatus.mte_status_success)
  {
       throw new ApplicationException($"Failed to initialize the MTE Decoder engine.  Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}");
  }
   }
   ```

   *(For further info on Decoder constructor – DevelopersGuide)*

   Here is a sample function that creates the MTE Encoder.

   ```c#
   public static void InstantiateEncoder()
   {
     _encoder.SetEntropy(Encoding.UTF8.GetBytes(_encoderEntropy));
  _encoder.SetNonce(_encoderNonce);

  _encoderStatus = _encoder.Instantiate(_identifier);

  if (_encoderStatus != MteStatus.mte_status_success)
     {
       throw new ApplicationException($"Failed to initialize the MTE Encoder engine. Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}");
     }
   }
   ```

*(For further info on Encode constructor – DevelopersGuide)*



  Call the MTE Decoder and MTE Encoder functions.

   ```c#
   InstantiateEncoder();
   InstantiateDecoder();
   ```



8. **Finally, we need to add the MTE calls to encode and decode the messages that we are sending and receiving from the other side.**
   - Ensure on the server side the Encoder is called to encode the outgoing text, then the Decoder is called to decode the incoming response.

    Here is a sample of how to do this on the server side

    ```csharp
    // Decode incoming message
    decodedMessage = _decoder.DecodeStr(newBuffer, out mteStatus);
   
    if (mteStatus != MteStatus.mte_status_success)
    {
      Console.WriteLine($"Error decoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
      _tokenSource.Cancel();
      break;
    }
   
    // Encode outgoing response
    encodedMessage = _encoder.Encode(decodedMessage, out mteStatus);
   
    if (mteStatus != MteStatus.mte_status_success)
    {
      Console.WriteLine($"Error encoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
      _tokenSource.Cancel();
      break;
    }
    ```



Here is a sample of how to do this on the client side

```csharp
// Encode outgoing message
encodedMessage = _encoder.Encode(message, out mteStatus);

if (mteStatus != MteStatus.mte_status_success)
{
  Console.WriteLine($"Error encoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
  tokenSource.Cancel();
  break;
}

// Decode incoming response
decodedMessage = _decoder.DecodeStr(newBuffer, out mteStatus);

if (mteStatus != MteStatus.mte_status_success)
{
  Console.WriteLine($"Error decoding: Status: {_mteBase.GetStatusName(mteStatus)} / {_mteBase.GetStatusDescription(mteStatus)}");
  tokenSource.Cancel();
  break;
}
```







***The Server side and the Client side of the MTE Sockets Tutorial should now be ready for use on your device.***


<div style="page-break-after: always; break-after: page;"></div>

# Contact Eclypses

<img src="Eclypses.png" style="width:8in;"/>

<p align="center" style="font-weight: bold; font-size: 22pt;">For more information, please contact:</p>
<p align="center" style="font-weight: bold; font-size: 22pt;"><a href="mailto:info@eclypses.com">info@eclypses.com</a></p>
<p align="center" style="font-weight: bold; font-size: 22pt;"><a href="https://www.eclypses.com">www.eclypses.com</a></p>
<p align="center" style="font-weight: bold; font-size: 22pt;">+1.719.323.6680</p>

<p style="font-size: 8pt; margin-bottom: 0; margin: 300px 24px 30px 24px; " >
<b>All trademarks of Eclypses Inc.</b> may not be used without Eclypses Inc.'s prior written consent. No license for any use thereof has been granted without express written consent. Any unauthorized use thereof may violate copyright laws, trademark laws, privacy and publicity laws and communications regulations and statutes. The names, images and likeness of the Eclypses logo, along with all representations thereof, are valuable intellectual property assets of Eclypses, Inc. Accordingly, no party or parties, without the prior written consent of Eclypses, Inc., (which may be withheld in Eclypses' sole discretion), use or permit the use of any of the Eclypses trademarked names or logos of Eclypses, Inc. for any purpose other than as part of the address for the Premises, or use or permit the use of, for any purpose whatsoever, any image or rendering of, or any design based on, the exterior appearance or profile of the Eclypses trademarks and or logo(s).
</p>
