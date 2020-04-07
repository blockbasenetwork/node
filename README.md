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

## Smart Contracts
**blockbaseopr** - Operations Contract
**blockbasetkn** - Token Contract



## Example: Running a chain with 3 producers
**Note: Public networks where the BlockBase contracts are currently deployed are the Jungle and Kylin test networks.**

You’ll need to run four different instances of the BlockBase node (either in different machines or in the same machine for testing purposes, as long as they’re listening on different ports).

You also need four different EOS accounts in the EOSIO network where you’re going to run the nodes, these need to be configured in the appsettings.json file inside the BlockBase.Node folder, alongisde the ports they’re using for the peer to peer connectinos. For the moment, you’ll need to use the key of the active permission of this account in order to run the node, we suggest creating accounts for the sole purpose of running BlockBase nodes. We also plan to add support for custom permissions in future updates.

Next, you need to have some BBT in order do add stake to your new chain. To get BBT on the mainnet you can participate in our Airgrabs, in order to get BBT in Jungle or Kylin just drop a message in our Telegram channel.

Adding stake is done through the addstake action in the BlockBase token contract, which can be used with cleos like the following examples:

Staking as a requester:

`cleos -u `_`network_endpoint`_` push action blockbasetkn addstake '[`_`sidechainaccount`_`,`_`sidechainaccount`_`,"X.XXXX BBT", ""]'`

Staking as a provider:

`cleos -u `_`network_endpoint`_` push action blockbasetkn addstake '[`_`provideraccount`_`,`_`sidechainaccount`_`,"X.XXXX BBT", ""]'`

The ammount to stake should be higher than what you plan to set as a minimum stake as a requester. The providers can add the stake after the node is running and the sidechain has been configured.

After configuring each node, run the four nodes:

`dotnet run --project `_`BlockBase.Node_Folder`_` --urls=`_`Api_Endpoint`_

Next you’ll need to start and configure the chain, using the requests to the api endpoint explained in the “Creating a new sidechain” section above.

For the providers, you’ll need to follow the “Sending a candidature for a sidechain” section above for each one.

If everything was configured correctly, the BlockBase sidechain should start after the candidature period is over.