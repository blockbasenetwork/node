# Introduction

## Main concepts
The BlockBase node can be run for two different purposes:
1. To run as a service requester (SR): running the node as a service requester allows you to tap into the network and to issue a sidechain to be stored by service providers. That sidechain will hold all your database data. You can use the node APIs to insert, update, delete, and query data. Running a node as a SR can be viewed as something similar to running a database server. SRs pay with BBT (the BlockBase token) for this service.

2. To run as a service provider (SP): running the node as a service provider allows you to participate on the network to store sidechains from SRs. The SPs earn BBT for providing this service.

## Development State
The node software is in beta and in active testing on the EOS network. The software is usable, but will probably still have bugs. Use with care and avoid large sums of BBT.

# Installation Guide
Here you can find all the steps to run a node as a service requester or as a service provider. This installation guide is tailored to a Linux installation, but it should work on Windows too.

## EOS Accounts
Each instance of the node has to have an EOS account associated to it. We recommend using a new EOS account just for that purpose. This account must have enough RAM, CPU, and NET to work properly. We recommend the following steps to prepare your EOS account:
1. Create the EOS account: An EOS account can be easily created on bloks.io [here](https://bloks.io/wallet/create-account).

2. Buy RAM: Buy 10k of RAM for your account.

3. Get CPU and NET: A BlockBase node uses a lot of CPU and a good amount of NET. We recommend buying renting the required CPU and NET through REX. To learn more about REX click [here](https://eosauthority.com/rex_history/).

4. Ensure you have always enough CPU and NET: Buying REX rents you CPU and NET for one month only, so this could pose a future problem for your node because it may run out of resources. To ensure your node has always enough resources, we recommend the [Charm service by Chintai](https://arm.chintai.io/). You can very easily configure this service to always buy REX for your account when you need the resources. We use it on our nodes and we highly recommend them.

## Software Prerequisites
The BlockBase node software is built in C# on the .NET Core Platform, and uses MongoBD and Postgres to store its data. Before running the node, you should install:
- .NET Core SDK 2.1 (BlockBase doesn't run on 3.1)
- The latest version of MondoDB Server (It should work fine with previous versions too)
- The latest version of PostgreSQL (It should work fine with previous versions too)

## Configuring the Node
Inside BlockBase.Node/appsettings.json you'll find all the settings you need to configure in order to run the BlockBase node.

```js
{
  "NodeConfigurations": {
    "AccountName": "", // The node's EOS account name
    "ActivePrivateKey": "", // The private key for the active permission key of the node account
    "ActivePublicKey": "", // The public key for the active permission key
    "SecretPassword": "", // The secret passphrase that will be used when choosing candidates to produce a sidechain
    "MongoDbConnectionString": "mongodb://localhost", // MongoDB connection string
    "MongoDbPrefix": "blockbase", // A prefix that will be used in all created MongoDB databases
    "PostgresHost": "localhost", // The postgresql host address
    "PostgresUser": "postgres", // The postgres user name to use for the connection
    "PostgresPort": 5432, // The port to use in the postgres connection
    "PostgresPassword": "yourpassword", // The password for the user used
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

`dotnet run --project `_`BlockBase.Node_Folder`_` --urls=`_`This_Node_Api_Endpoint`_

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
if you intend on running the node as a service provider, you can use the following action to send a candidature to a sidechain:

`https://`_`apiendpoint`_`/api/Producer/SendCandidatureToChain?chainName=`_`ChainName`_`&workTime=`_`WorkTimeInSeconds`_`&producerType=`_`producerType`_

Where workTime is the amount of time in seconds the producers will work on the chain, and producerType is the type of producer it intends to be. ProducerType may assume one of three numbers: 1, 2 and 3. This will determine the level of the producer. 1 is only a node that validates blocks and doesn't build the sidechain, 2 is a node that also produces the sidechain, and 3 is a node that produces the sidechain and executes the operations on a local database.


## Staking BBT
In order to stake BBT as a service requester, run the 'addstake' action in the blockbase token contract with both 'owner' and 'sidechain' with your sidechain account name.

In case you want to stake as a service provider, run the 'addstake' action with 'owner' as your producer accout name and 'sidechain' as the sidechain you want to candidate as a producer.

## Smart Contracts
**blockbaseopr** - Operations Contract
**blockbasetkn** - Token Contract



## Example: Running a chain with 3 producers
**Note: Public networks where the BlockBase contracts are currently deployed are the EOS Mainnet, as well as Jungle and Kylin test networks.**

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



## Querying BlockBase sidechains

To query a BlockBase sidechain, you need to send the query to the following node endpoint of the service requester:

`https://`_`apiendpoint`_`/api/Query/ExecuteQuery/`

With the query string inside the body of the POST request.

### Query Examples

#### Database

To create, drop or use a database, the commands used are the same as you would see in the SQL Language:

```
CREATE DATABASE { Name of Database } – Used to create the database with a custom name
DROP DATABASE { Name of Database } - Used to drop a previously created database.
USE { Name of Database } - Use to specify the database you want to query.
```

**Note: After creating a database, it's always required to have the USE statement preceding any other query to the database,**

#### Table

##### Create Table

To create a table in BBSQL, the commands used once more follow the SQL sintax, with the addition of some options added by using the reserved word “ENCRYPTED”, which allows adding the number of buckets and/or the RANGE to use in the encryption of the informations:

`CREATE TABLE {Table name} ( {Column_Definition} )`

Example:

`CREATE TABLE invoice ( invoice_id int PRIMARY KEY, customer_id ENCRYPTED 5 RANGE(1, 1000) )`


Column Definition:

`{Column_Name} {Type} [{constraint}*]`

Examples:

`invoice_id int PRIMARY KEY`
`customer_id ENCRYPTED 5 RANGE(1, 1000)`

The value after the reserved word ENCRYPTED is the 'number of buckets' for that column. If the number of buckets is 5, this means all records will fall into 5 buckets and record retrieval will be done by bucket and not by record value. So, the larger the number the quicker it is to retrieve records, but more information regarding the frequency of values is leaked.

**An exclamation mark '!' before a table name means the name of the column is unencrypted. If you want to encrypt the table name remove the ! Character**

`[!] {Name}`

Examples:

Here's an example of a simple table creation with some of the records encrypted. This means they are fully encrypted on the client-side:

`CREATE TABLE staff(!id int PRIMARY KEY , !name ENCRYPTED 4 NOT NULL, !email TEXT, !position TEXT, yearOfBirth ENCRYPTED 4 RANGE(10,1905, 2999) NOT NULL, !address ENCRYPTED 4 NOT NULL, socialSecurity ENCRYPTED 4 NOT NULL, salary ENCRYPTED 5 RANGE(500,1000, 20000));`

**IMPORTANT:**
- An exclamation mark '!' before a table name means the name of the column is unencrypted.
- Notice the name column, it has the word 'ENCRYPTED' right after it and then the number 4. This means that the data of this column will be encrypted. Furthermore, it also means that encrypted records will be associated to 4 buckets. The more buckets you have the faster you can query data but the more you reveal their frequency.
- With this configuration you can query records with equality queries '=' and '!=', and bbsql will query records by their buckets instead of the record values themselves.
- Notice the yearOfBirth column, besides the word 'ENCRYPTED' and the bucket number, it also has 'RANGE(10, 1905, 2999)'. With this configuration you can also make range queries using '<', '<=', '>' and '>=', and it states the range 1905 to 2999 will be devided into 10 consecutive buckets.


##### Alter Table

Syntax:

```
ALTER TABLE {Table Name} RENAME TO {New Table Name};
ALTER TABLE {Table Name} RENAME COLUMN {Column Name} TO {New Column Name};
ALTER TABLE {Table Name} DROP COLUMN {Column Name}; 
ALTER TABLE {Table Name} ADD COLUMN {Column Name};
```

Examples:

```
ALTER TABLE invoiice RENAME TO invoice
ALTER TABLE invoice RENAME COLUMN id TO !id
ALTER TABLE product ADD COLUMN name TEXT
ALTER TABLE product DROP COLUMN name
```

##### Drop Table

Syntax:

`DROP TABLE { Table_Name }`


#### Records

##### Insert Record

Syntax: 

`INSERT INTO {Table_Name} VALUES ( {Column_Name}* ) VALUES ( {Literal_Value}* )`

Examples:

`INSERT INTO product (id, name, price) VALUES (1, ‘sponge’, 5.00)`
`INSERT INTO clients(id, name, email, deliveryAddress, billingAddress, zipCode) VALUES (0, 'Mary','mary@bb.com','123 Mary Street','123 Mary Street','85035')`

##### Delete Record

Syntax:

`DELETE FROM {Table_Name} [WHERE {expression}]`

Example:

`DELETE FROM product WHERE name = ‘sponge’`

##### Update Record

Syntax:

`UPDATE {Table_Name} SET ( {Column_Name} = {Literal_Value} )* [WHERE {Expression}]`

Example:

`UPDATE product SET price = 6.00 WHERE Name = ‘sponge’`


##### Select Record

The select command is used to retrieve data from the database ranging from simple commands to complex ones. This command is similiar to the SELECT in the SQL Language, being able to filter the information as you please. This command follows this formula:

`SELECT ( {Table_Name}.{Column_Name} )* FROM {table_name}* [ JOIN {table_name} ON {Expression} ]* [ WHERE {expression} ] [ ENCRYPTED ]`

Examples:

`SELECT product.* FROM product WHERE price < 10.00`

```
– An example of a inner join between patients table and visitors table, to find all
SELECT patients.name, patients.dateOfAdmission, visitors.name, visitors.dateOfVisit FROM patients JOIN visitors ON patients.id = visitors.patientVisited WHERE patients.id = 2;
```

```
– here's an example on how you get data all staff with salary equal or lower tha n 10000 
SELECT staff.* FROM staff where staff.salary {= 10000;
```

```
– here you can see the difference between getting data encrypted or decrypted
SELECT patients.* FROM patients;
SELECT patients.* FROM  patients ENCRYPTED;
```

```
– this is a simple statment to get all the data, without encryption
SELECT visitors.* FROM visitors;
```

For more examples visit : https://www.blockbase.network/SandBox
