# Introduction
BlockBase is the Power of Blockchain applied to Databases. It’s a distributed network of nodes running the BlockBase Node code. This distributed network builds sidechains to the EOS network, which are used to store databases on them.

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

5. Transfer BBT (The BlockBase token) to that account. You will need BBT as a SR or a SP. SRs use BBT to pay to SPs for running their sidechain. And SPs pledge BBT as collateral that they will lose if they fail to provide the service accordingly. In both cases BBT has to be staked.

**Note: If you wish to run more than one instance of the node, you will need a different EOS account for every one of those instances.**

## Software Prerequisites
The BlockBase node software is built with C# and runs on the .NET Core Platform, and uses MongoBD and Postgres to store its data. Before running the node, you should install:
1. .NET Core SDK 2.1 (BlockBase doesn't run on 3.1)

2. The latest version of MondoDB Server (It should work fine with previous versions too)

3. The latest version of PostgreSQL (It should work fine with previous versions too)

## Configuring the Node
Inside BlockBase.Node/appsettings.json you'll find all the settings you need to configure in order to run the BlockBase node.

```js
{
  "NodeConfigurations": {
    "AccountName": "", // The EOS account name you configured
    "ActivePrivateKey": "", // The private key for the active permission key of the node account
    "ActivePublicKey": "", // The public key for the active permission key
    "MongoDbConnectionString": "mongodb://localhost", // The MongoDB connection string
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
## Downloading the code and running the node
To download the code and run the node follow the following steps:
1. Create a folder where the code will be downloaded to

2. Open a terminal on that folder and run `git clone https://github.com/blockbasenetwork/node.git`

3. Navigate to the folder node/BlockBase.Node

4. Run the code with the command `dotnet run --urls=localhost:5000` (this is just an example url, change it accordingly to your needs)

5. Open a browser and navigate to the link you set as parameter for urls. A swagger UI interface should appear.

## Checking if everything is correctly configured
With the node running, the first thing you should check is if everything is correctly configured. Follow these steps to check if the node is correctly configured:
1. On the upper right side of the swagger page choose the "Service Requester" API from the list of available APIs.

2. Click on `/api/Requester/CheckRequesterConfig` then on `Try it out` and then on `Execute`.

3. Inspect the response. It should have a code `200`. Inside the details of the response, check if `"succeeded":true`, `"accountDataFetched":true`, `"isMongoLive":true` and `"isPostgresLive":true`. All these values should be set to true. If not, there is a problem with you configuration.

# Running a Node as a Service Requester
Running a node as a SR allows you to store your data on the BlockBase Network. To do this, you have to follow the steps below.

## Step #1 - Configuring the sidechain
Before you can request the network for a new sidechain, you have to configure the parameters of the sidechain. If your node is running, you have to stop it by pressing `Ctrl+c`. After you've stopped the node, navigate back to the appsettings file on BlockBase.Node/appsettings.json . There, you need to edit the following section:

```js
{
    "RequesterConfigurations": {
    "MaxPaymentPerBlockValidatorProducers": 10.0, // The payment in BBT each validator node will receive when he produces a block filled to the max with transactions
    "MaxPaymentPerBlockHistoryProducers": 100.0, // The payment in BBT each history node will receive when he produces a block filled to the max with transactions
    "MaxPaymentPerBlockFullProducers": 0, // The payment in BBT each full node will receive when he produces a block filled to the max with transactions
    "MinimumPaymentPerBlockValidatorProducers": 1.0, // The payment in BBT each validator node will receive when he produces a block that has no transactions
    "MinimumPaymentPerBlockHistoryProducers": 1.0, // The payment in BBT each history node will receive when he produces a block that has no transactions
    "MinimumPaymentPerBlockFullProducers": 0, // The payment in BBT each full node will receive when he produces a block that has no transactions
    "MinimumCandidatureStake": 100.0, // The minimum stake each node has to have staked as collateral on the sidechain
    "NumberOfValidatorProducersRequired": 1, // The desired number of validator nodes
    "NumberOfHistoryProducersRequired": 3, // The desired number of history nodes
    "NumberOfFullProducersRequired": 0, // The desired number of full nodes
    "BlockTimeInSeconds": 60, // The time in seconds between the production of each block
    "NumberOfBlocksBetweenSettlements": 10, // The number of blocks between each settlement. Its during settlements that payments are made
    "BlockSizeInBytes": 1000000, // The maximum size of a block in bytes
    "ReservedProducerSeats": ["producer1eosaccount", "producer2eosaccount"]
  }
}
```
### Understanding the costs
The way you configure your sidechain request is very important. You need to understand the costs involved for you and for the service providers. If the sidechain you request demands too much from a SP and you pay too little, no SP you will want to participate in your sidechain. Inversely, if you pay too much to a SP and demand too little from him, you will be squandering resources. To get an idea of how much your sidechain will cost, [visit our sidechain costs calculator](https://blockbase.network/costs-calculator).

### Choosing the right network
The number of nodes you want for your sidechain depends on the security you need and how much you're willing to spend. As you can see in the configuration, there are three types of providers: Validator Nodes, History Nodes, and Full Nodes. Validator nodes **do not** need to store the whole sidechain on their side. They just help in block production. History Nodes **have to** store the whole sidechain on their side, and are checked on every settlement. Full nodes have to store the sidechain and the resulting databases from executing all operations on the sidechains. *Full nodes are not yet fully implemented*.

# Running as a service provider
## Sending a candidature for a sidechain
if you intend on running the node as a service provider, you can use the following action to send a candidature to a sidechain:

`https://`_`apiendpoint`_`/api/Producer/SendCandidatureToChain?chainName=`_`ChainName`_`&workTime=`_`WorkTimeInSeconds`_`&producerType=`_`producerType`_

Where workTime is the amount of time in seconds the producers will work on the chain, and producerType is the type of producer it intends to be. ProducerType may assume one of three numbers: 1, 2 and 3. This will determine the level of the producer. 1 is only a node that validates blocks and doesn't build the sidechain, 2 is a node that also produces the sidechain, and 3 is a node that produces the sidechain and executes the operations on a local database.


# Staking BBT
In order to stake BBT as a service requester, run the 'addstake' action in the blockbase token contract with both 'owner' and 'sidechain' with your sidechain account name.

In case you want to stake as a service provider, run the 'addstake' action with 'owner' as your producer accout name and 'sidechain' as the sidechain you want to candidate as a producer.

# Smart Contracts
**blockbaseopr** - Operations Contract
**blockbasetkn** - Token Contract



# Example: Running a chain with 3 producers
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
