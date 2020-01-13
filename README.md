# BlockBase Node App
BlockBase is the power of Blockchain applied to Databases. It uses sidechains for database storage, and those sidechains are connected to the EOS platform through EOS smart contracts.

## Development State
The node software currently has working consensus tested on private and public test networks. The database functionality is still in active development and not currently ready to be used.

## Prerequisites
- .NET Core SDK 2.1
- MondoDB Server 4.2.2
- PostgreSQL 12

## Configuring the Node
Inside BlockBase.Node/appsettings.json you'll find all the settings you need to configure in order to run the BlockBase node.

```json
{
  "NodeConfigurations": {
    "AccountName": "blockbase", // The node's EOS account name
    "ActivePrivateKey": "", // The private key for the active permission key of the node account
    "ActivePublicKey": "", // The public key for the active permission key
    "SecretPassword": "secret", // The secret passphrase that will be used when choosing candidates to produce a sidechain
    "MongoDbConnectionString": "mongodb://localhost", // MongoDB connection string
    "MongoDbPrefix": "blockbase" // A prefix that will be used in all created MongoDB databases
  },
  "NetworkConfigurations": {
    "LocalIpAddress": "127.0.0.1", // The IP address that other producers will connect to
    "LocalTcpPort": 4444, // The TCP port used to connect
    "EosNet": "http://127.0.0.1:8888", // EOS network endpoint
    "ConnectionExpirationTimeInSeconds": 15, // Connection expiration time
    "MaxNumberOfConnectionRetries": 3, // Number of connection retries
    "BlockBaseOperationsContract": "blockbaseopr", // The account running the BlockBase operations contract
    "BlockBaseTokenContract": "blockbasetkn" // The account running the BlockBase token contract
  }
}
```

## Running the node
Run the following command to get the node up and running:

`dotnet run --project `_`BlockBase.Node_Folder`_` --urls=`_`Api_Endpoint`_

## Creating a new sidechain
In case you want to run the node as a service requester, make a request to the following action in order to start a new sidechain:

`https://`_`apiendpoint`_`/api/Chain/StartChain`

To configure the chain, a POST request is needed to the following action:

`https://`_`apiendpoint`_`/api/Chain/ConfigureChain`

With the following body:

```json
{
	"key": "blockbasedb1", // The name of the chain
	"paymentperblock": 400, // The payment for each block, in the lowest decimal value of BBT (ie: 400 means that each block will cost 0.0400 BBT)
	"minimumcandidatestake": 100, // The minimum stake each producer needs to have in sidechain in the lowest decimal value of BBT
	"requirednumberofproducers":3, // The desired number of producers for the sidechain
	"candidaturetime":90, // The candidature phase time in seconds
	"sendsecrettime":20, // The send secret phase time in seconds
	"ipsendtime":20, // The send IP phase time in seconds
	"ipreceivetime":20, // The retrieve IP phase time in seconds
	"candidatureenddate":0, // No need to change
	"secretenddate":0, // No need to change
	"ipsendenddate":0, // No need to change
	"ipreceiveenddate":0, // No need to change
	"blocktimeduration": 180, // Time between each block in seconds
	"blocksbetweensettlement": 20, // The number of blocks between each settlement phase
	"sizeofblockinbytes":1000 // The maximum size of each block in bytes
}
```

Finally, a request is needed to the following action in order to get the chain running:

`https://`_`apiendpoint`_`/api/Chain/StartChainMaintainance`

## Sending a candidature for a sidechain
if you intent on running the node as a service provider, you can use the following action to send a candidature to a sidechain:

`https://`_`apiendpoint`_`/Producer/SendCandidatureToChain?chainName=`_`ChainName`_`&workTime=`_`WorkTimeInSeconds`_