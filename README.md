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

```js
{
  "NodeConfigurations": {
    "AccountName": "blockbase", // The node's EOS account name
    "ActivePrivateKey": "", // The private key for the active permission key of the node account
    "ActivePublicKey": "", // The public key for the active permission key
    "SecretPassword": "secret", // The secret passphrase that will be used when choosing candidates to produce a sidechain
    "MongoDbConnectionString": "mongodb://localhost", // MongoDB connection string
    "MongoDbPrefix": "blockbase", // A prefix that will be used in all created MongoDB databases
    "PostgresHost": "localhost", // The postgresql host address
    "PostgresUser": "postgres", // The postgres user name to use for the connection
    "PostgresPort": 5432, // The port to use in the postgres connection
    "PostgresPassword": "blockbase", // The password for the user used
    "EncryptionMasterKey": "f3e7frm53rmyb6bn9jbyeopkgyz3jcwotexdfgnexbcjyz8sswwo",
    "EncryptionPassword": "qwerty123"
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

**Note: If you wish to run the node as a sidechain requester and sidechain provider, you will need two different instances and two different EOS accounts.**

**Note 2: You will need to have staked BBT on the sidechains in order get the services running, more details below**


## Running as a service requester
### Creating a new sidechain
In case you want to run the node as a service requester, make a request to the following action in order to start a new sidechain:

`https://`_`apiendpoint`_`/api/Chain/StartChain`

To configure the chain, a POST request is needed to the following action:

`https://`_`apiendpoint`_`/api/Chain/ConfigureChain`

With the following body:

```js
{
	"key": "blockbasedb1", // The name of the chain
	"payment_per_block_validator_producers": 1, // The payment for each block for validator producers, in the lowest decimal value of BBT (ie: 400 means that each block will cost 0.0400 BBT)
	"payment_per_block_history_producers": 1, // The payment for each block for history producers
	"payment_per_block_full_producers": 1, // The payment for each block for full producers
	"min_candidature_stake": 100, // The minimum stake each producer needs to have in sidechain in the lowest decimal value of BBT
	"number_of_validator_producers_required":1, // The desired number of validator producers for the sidechain
	"number_of_history_producers_required":1, // The desired number of history producers for the sidechain
	"number_of_full_producers_required":1, // The desired number of full producers for the sidechain
	"candidature_phase_duration_in_seconds":90, // The candidature phase time in seconds
	"secret_sending_phase_duration_in_seconds":20, // The send secret phase time in seconds
	"ip_sending_phase_duration_in_seconds":20, // The send IP phase time in seconds
	"ip_retrieval_phase_duration_in_seconds":20, // The retrieve IP phase time in seconds
	"candidature_phase_end_date_in_seconds":0, // No need to change
	"secret_sending_phase_end_date_in_seconds":0, // No need to change
	"ip_sending_phase_end_date_in_seconds":0, // No need to change
	"ip_retrieval_phase_end_date_in_seconds":0, // No need to change
	"block_time_in_seconds": 180, // Time between each block in seconds
	"num_blocks_between_settlements": 20, // The number of blocks between each settlement phase
	"block_size_in_bytes":1000 // The maximum size of each block in bytes
}
```

Finally, a request is needed to the following action in order to get the chain running:

`https://`_`apiendpoint`_`/api/Chain/StartChainMaintenance`



## Running as a service provider
### Sending a candidature for a sidechain
if you intent on running the node as a service provider, you can use the following action to send a candidature to a sidechain:

`https://`_`apiendpoint`_`/Producer/SendCandidatureToChain?chainName=`_`ChainName`_`&workTime=`_`WorkTimeInSeconds`_`&producerType=`_`producerType`_

Where workTime is the ammount of time in seconds the producers will work on the chain, and producerType is the type of producer it intends to be.


## Staking BBT
In order to stake BBT as a service requester, run the 'addstake' action in the blockbase token contract with both 'owner' and 'sidechain' with your sidechain account name.

In case you want to stake as a service provider, run the 'addstake' action with 'owner' as your producer accout name and 'sidechain' as the sidechain you want to candidate as a producer.

## Smart Contracts running on Jungle
**blockbaseopr** - Operations Contract
**blockbasetkn** - Token Contract
