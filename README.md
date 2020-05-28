# Introduction
BlockBase is the Power of Blockchain applied to Databases. It’s a distributed network of nodes running the BlockBase Node code. This distributed network builds sidechains to the EOS network, which are used to store databases on them.

## Main concepts
The BlockBase node can be run for two different purposes:
1. To run as a service requester (SR): running the node as a service requester allows you to tap into the network and issue a sidechain to be stored by service providers. That sidechain will hold all data of your databases. You can use the node APIs to insert, update, delete, and query data. Running a node as a SR can be viewed as something similar to running a database server. SRs pay with BBT (the BlockBase token) for this service.

2. To run as a service provider (SP): running the node as a service provider allows you to participate on the network to store sidechains for SRs. The SPs earn BBT for providing this service.

## Development State
The node software is in beta and in active testing on the EOS network. The software is usable, but will probably still have bugs. Use with care and avoid large sums of BBT.

## Smart Contracts
If you are curious about which smart contracts are used, you can find them on bloks.io here [blockbaseopr - Operations Contract](https://bloks.io/account/blockbaseopr) and [blockbasetkn - Token Contract](https://bloks.io/account/blockbasetkn).

# Installation Guide
Here you can find all the steps to run a node as a [service requester](#Running-a-Node-as-a-Service-Requester) or as a [service provider](#Running-as-a-service-provider). This installation guide is tailored to a Linux installation, but it should work on Windows too.

## EOS Accounts
Each node has to have an EOS account associated to it. We recommend using a new EOS account just for that purpose. This account must have enough RAM, CPU, and NET to work properly. We recommend the following steps to prepare your EOS account:
1. Create the EOS account: An EOS account can be easily created on bloks.io [here](https://bloks.io/wallet/create-account).

2. Buy RAM: Buy 10k of RAM for the EOS account you created.

3. Get CPU and NET: A BlockBase node uses a lot of EOS CPU and a good amount of EOS NET. We recommend renting the required CPU and NET through REX. To learn more about REX click [here](https://eosauthority.com/rex_history/).

4. Ensure the EOS account has always enough CPU and NET: Renting CPU and NET through REX lasts for one month only, so this could pose a future problem for your node because it may run out of resources. To ensure your node has always enough resources, we recommend the [Charm service by Chintai](https://arm.chintai.io/). You can very easily configure this service to always buy REX for your account when you need the resources. We use it on our nodes and we highly recommend it.

5. Transfer BBT (The BlockBase token) to the EOS account. You will need BBT as a SR or as a SP. SRs use BBT to pay to SPs for running their sidechain. And SPs pledge BBT as collateral that they will lose if they fail to provide the service accordingly. In both cases BBT has to be staked.

**Note: If you wish to run more than one instance of the node, you will need a different EOS account for every one of those instances.**

## Software Prerequisites
The BlockBase node software is built with C# and runs on the .NET Core Platform, and uses MongoBD and PostgreSQL to store its data. Before running the node, you should install:
1. .NET Core SDK 2.1 (The current version of BlockBase doesn't run on 3.1)

2. The latest version of MongoDB Server (It should work fine with previous versions too)

3. The latest version of PostgreSQL (It should work fine with previous versions too)

## Downloading the code
To download the code follow these steps:
1. Create a folder where the code will be downloaded to

2. Open a terminal on that folder and run `git clone https://github.com/blockbasenetwork/node.git`

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
    "PostgresUser": "postgres", // The postgresql user name to use for the connection
    "PostgresPort": 5432, // The port to use in the postgresql connection
    "PostgresPassword": "yourpassword", // The password for the postgresql user
  },
  "NetworkConfigurations": {
    "PublicIpAddress": "127.0.0.1", // The public IP address that other producers will connect to
    "LocalTcpPort": 4440, // The TCP port used to connect
    "EosNet": "http://api.eosn.io", // A Top 21 EOS Network producer endpoint
    "ConnectionExpirationTimeInSeconds": 15, // (no need to change) Connection expiration time
    "MaxNumberOfConnectionRetries": 3, // (no need to change) Number of connection retries
    "BlockBaseOperationsContract": "blockbaseopr", // (no need to change) The account running the BlockBase operations contract
    "BlockBaseTokenContract": "blockbasetkn" // (no need to change) The account running the BlockBase token contract
  }
}
```

## Checking if everything is correctly configured
The first thing you should check is if everything is correctly configured. Follow these steps to check if the node is correctly configured:
1. Navigate to the folder node/BlockBase.Node

2. Run the code with the command `dotnet run --urls=http://localhost:5000` (this is just an example url, change it accordingly to your needs)

3. Open a browser and navigate to the link you set as parameter for urls. A swagger UI interface should appear.

4. On the upper right side of the swagger page choose the "Service Requester" API from the list of available APIs.

5. Click on `/api/Requester/CheckRequesterConfig` then on `Try it out` and then on `Execute`.

6. Inspect the response. It should have a code `200`. Inside the details of the response, check if `"succeeded":true`, `"accountDataFetched":true`, `"isMongoLive":true` and `"isPostgresLive":true`. All these values should be set to true. If not, there is a problem with your configuration.

# Running a Node as a Service Requester
Running a node as a SR allows you to store your data on the BlockBase Network. To do this, you have to follow the steps below.

## Step #1 - Configuring the sidechain
Before you can request the network for a new sidechain, you have to configure the parameters of the sidechain. If your node is running, you have to stop it by pressing `Ctrl+c`. After you've stopped the node, navigate back to the appsettings file on BlockBase.Node/appsettings.json . There, you need to edit the following section:

```js
{
  "RequesterConfigurations": {
    "ValidatorNodes": {
      "RequiredNumber": 0, // The required number of validator nodes
      "MaxPaymentPerBlock": 0, // The payment in BBT each validator node will receive when he produces a block filled to the max with transactions
      "MinPaymentPerBlock": 0 // The payment in BBT each validator node will receive when he produces a block that has no transactions
    },
    "HistoryNodes": {
      "RequiredNumber": 0, // The required number of history nodes
      "MaxPaymentPerBlock": 0, // The payment in BBT each history node will receive when he produces a block filled to the max with transactions
      "MinPaymentPerBlock": 0 // The payment in BBT each history node will receive when he produces a block that has no transactions
    },
    "FullNodes": {
      "RequiredNumber": 0, // The required number of full nodes
      "MaxPaymentPerBlock": 0, // The payment in BBT each full node will receive when he produces a block filled to the max with transactions
      "MinPaymentPerBlock": 0 // The payment in BBT each full node will receive when he produces a block that has no transactions
    },
    "MinimumProducerStake": 0, // The minimum stake each node has to provide as collateral to apply to participation
    "BlockTimeInSeconds": 60, // The time in seconds between the production of each block
    "NumberOfBlocksBetweenSettlements": 10, // The number of blocks between each settlement. Its during settlements that payments are made
    "MaxBlockSizeInBytes": 1000000, // The maximum size of a block in bytes
    "ReservedProducerSeats": ["producer1eosaccount", "producer2eosaccount"] // A list of EOS accounts you want to pre-select as service providers for your network
  }
}
```
### Understanding the costs
The way you configure your sidechain request is very important. You need to understand the costs involved for you and for the service providers. If the sidechain you request demands too much from a SP and you pay too little, no SP you will want to participate in your sidechain. Inversely, if you pay too much to a SP and demand too little from him, you will be squandering resources. To get an idea of how much your sidechain will cost, [visit our sidechain costs calculator](https://blockbase.network/costs-calculator).

### Choosing the right network
The number of nodes you want for your sidechain depends on the security you need and how much you're willing to spend. As you can see in the configuration, there are three types of providers: *Validator Nodes*, *History Nodes*, and *Full Nodes*. Validator nodes **do not** need to store the whole sidechain on their side. They just help in block production. History Nodes **have to** store the whole sidechain on their side, and are checked on every settlement. Full nodes have to store the sidechain and the resulting databases from executing all operations on the sidechains. _*Full nodes are not yet fully implemented*_.

### Adding reserved seats
In some cases, it may make sense to have a preselected list of accounts that will have a reserved spot for them on the network you're requesting. This is especially useful if you want your network to be partially or fully produced by nodes you manage. To add reserved seats, add the corresponding EOS accounts to the list of `ReservedProducerSeats`. A producer that has a reserved seat may participate as a validator, history, or full node. If more producers apply than the `RequiredNumber` for that type of producer, the ones who register first will get the position and the remaining ones will be left out.

## Step #2 - Configuring the data security
BlockBase has an encryption layer built in that allows you to encrypt all your data before it is sent to the providers. You can configure the security on the appsettings file. Before you do that though, you have to consider the implications of doing so.

Storing all your encryption passwords on your appsettings file poses a security problem. Furthermore, the owner of the data may not be the requester of the sidechain, and may want to keep the encryption passwords on his side. For that reason, you may choose to leave this section unconfigured. If you choose to do so, you have to consider the following consequences:
1. That security configuration will need to be provided further ahead when you *"Start the sidechain maintenance"* and that information will be stored only in memory.

2. The owner of the data will have to safely store the security configuration on another medium. **If the configuration is lost all access to all encrypted data will be lost**!

3. If the node or the machine its running on crash for some reason, when you restart the node you will have to go through the *"Start the sidechain maintenance"* step again and provide the configuration information again.

4. If you choose to leave the data security on your appsettings file, when the node is restarted it can automatically *"Start the sidechain maintenance"* step because it has all the data it needs.

If you wish to keep the data security configuration on your appsettings file here BlockBase.Node/appsettings.json, find the section below and edit it.

```js
{
  "securityConfigurations":
  {
  "useSecurityConfigurations": true, // indicates if the security configurations here should be used or ignored
  "filePassword": "string", // encrypts the contents of the file where the keys are going to get stored
  "encryptionMasterKey": "string", // a master key for generation of all encryption keys of all databases - you can generate one yourself with the GenerateMasterKey service
  "encryptionPassword": "string", // initial passphrase to generate the master IV
  }
}
```

The `encryptionMasterKey` has to be encoded in a zbase32 format. We use this format mainly for readability. The key should be generated through a random process though, and we provide a service for that. To generate a masterkey, follow these steps:

**Start the node** (If it's not running)
1. Navigate to the folder node/BlockBase.Node

2. Open a terminal there and run the command `dotnet run --urls=http://localhost:5000` (this is just an example url, change it accordingly to your needs)

**Generating a master key**
1. On the upper right side of the swagger page choose the "Service Requester" API from the list of available APIs.

2. Click on `/api/Requester/GenerateMasterKey` then on `Try it out` and then on `Execute`.

Inspect the response message.

```js
{
  "succeeded": true,
  "exception": null,
  "response": "<your master key should be here>",
  "responseMessage": "Master key successfully created. Master Key = <your master key should be here>"
}
```
3. Copy the master key and paste it on the value of the `encryptionMasterKey` on the `securityConfigurations` of the BlockBase.Node/appsettings.json file.


## Step #3 - Requesting the sidechain
After you've configured your sidechain, you can request it to the network. This will make your sidechain configuration public to all the providers on the network. To do that, follow these steps:

**Start the node** (If it's not running)
1. Navigate to the folder node/BlockBase.Node

2. Open a terminal there and run the command `dotnet run --urls=http://localhost:5000` (this is just an example url, change it accordingly to your needs)

**Requesting the sidechain**
1. Open a browser and navigate to the link you set as parameter for urls.

2. On the upper right side of the swagger page choose the "Service Requester" API from the list of available APIs.

3. Click on `/api/Requester/RequestNewSidechain` then on `Try it out` and then on `Execute`.

## Step #4 - Starting the sidechain maintenance
After you've announced your sidechain request, now you have to participate on the maintenance of the sidechain. Your node is responsible for keeping up with the providers, for sending them transactions with the data you will want to store, and for moving forward with the lifecycle of the sidechain.

Starting the maintenance of the sidechain is a fundamental step for your network to work. If for some reason your node crashes, after restarting the node you **have to start the maintenance of the sidechain again unless you configured your data security on the appsettings file as described on the section _Configuring the data security_**.

### Start the sidechain maintenance with the security on the appsettings file
1. Open a browser and navigate to the link you set as parameter for urls.

2. On the upper right side of the swagger page choose the "Service Requester" API from the list of available APIs.

3. Click on `/api/Requester/RunSidechainMaintenance` then on `Try it out` and then `Execute`

### Start the sidechain maintenance without the security on the appsettings file
If you didn't store your data security configuration on the BlockBase.Node/appsettings.json file, you will have to pass that configuration on the body of the request.

Unfortunately, for technical reasons you won't be able to do this through swagger so you will have to use an alternative method. We recommend you to use [Postman](https://www.postman.com/) for that. Follow these steps:
1. Download Postman.

2. Prepare a Post request to _`your_api_endpoint`_`/api/Requester/RunSidechainMaintenance/`
   
3. Copy the json content below and fill the parameters accordingly, and paste it on the body of the request. **Remember that these configurations won't be stored by the node and will have to be provided everytime the node is started. Store them safely or all your encrypted data won't be recoverable!**

```js
{
  {
  "filePassword": "string", // encrypts the contents of the file where the keys are going to get stored
  "encryptionMasterKey": "string", // master key for generation of all encryption keys of all databases
  "encryptionPassword": "string", // initial passphrase to generate master IV
  }
```

4. Click `Send`.

## Your node is configured and running
Your node is up and running, your sidechain has been requested to the network, and the maintenance of the sidechain is running too. Visit our [Network Explorer](https://blockbase.network/Tracker) and find your sidechain request there. Sometimes it takes a while to appear there.

# Running as a Service Provider
Running a node as a SP allows you to produce sidechains for SRs in exchange for BBT. Running the node as a SP is easier than as a SR. There are less steps involved. A SP has to worry mostly about the infrastructure, and about the costs/benefits of participating on building a sidechain that has been requested.

There are two main ways to apply to participate on producing sidechains. The first one is manual and the second one is automatic. With the manual way you can identify a sidechain that you want to participate on, read all its requirements, and decide if you want to apply or not. With the automatic way you can configure your node to be on the lookout for sidechain requests that fulfill your participation requirements. Both ways are explained further below.

### Participating on a sidechain means you have responsabilities
A sidechain request has information about the stake in BBT it requires from the providers in order to participate. This stake is a pledge in BBT as collateral that will be lost if the provider doesn't do his job right. A provider may stake more BBT than the required amount on the sidechain request. The main reason to do this is explained right below.

### Applying to participate on a sidechain doesn't mean your node will be selected
A sidechain that has been requested has a specified number of validator, history, and full nodes requested. If the number of nodes that apply to a certain position is higher than the number of requested nodes, some nodes will be left out. This process is done through a random elimination process that favors providers with the larger stake on the sidechain. The providers who are left out remain on a list of backup nodes that will start producing if one of the selected nodes leaves the network or is kicked out.

## Manually applying to participate on a sidechain
To participate on a sidechain manually the first thing you need to do is to find a sidechain that has been requested and is currently in a candidature phase. That means it is currently accepting providers to join. To participate on a sidechain manually, follow these steps:

**Start the node** (If it's not running)
1. Navigate to the folder node/BlockBase.Node

2. Open a terminal there and run the command `dotnet run --urls=http://localhost:5000` (this is just an example url, change it accordingly to your needs)

**Find a sidechain**
Go to our [Network Tracker](https://www.blockbase.network/Tracker) online and find a sidechain that is in a candidature phase and take note of the sidechain account name.

**Apply to participate**
1. Open a browser and navigate to the link you set as parameter for urls.

2. On the upper right side of the swagger page choose the "Service Provider" API from the list of available APIs.

3. Click on `/api/Producer/RequestToProduceSidechain` then on `Try it out`.

4. Fill the fields

	4.1. `chainName`: the name of the sidechain
	4.2. `stake`: the amount of stake in BBT you want to put as collateral
	4.3. `producerType`: the type of provider your node is going to be. *ProducerType may assume one of three numbers: 1, 2 and 3, which states the level of the producer. 1 is only a node that validates blocks and doesn't build the sidechain, 2 is a node that also builds the sidechain, and 3 is a node that builds the sidechain and executes the operations on a local database.*
  
5. Click `Execute`.

## Automatically applying to participate on sidechains
If you want to fire and forget your node you can configure it to automatically apply to sidechains that fulfill your participation requirements. To do that, you need to edit the BlockBase.Node/appsettings.json file and edit the configuration below.

**Note: if you change the configurations you will need to restart the node.**

By setting any of the `IsActive` active properties to `true`, when the node starts it will automatically try to find sidechains that meet your requirements.

```js
{
  "AutomaticProduction": {
    "ValidatorNode": {
      "IsActive": false, // Determines if the automatic production as a validator is active
      "MinBBTPerBlock": 0, // The minimum amount of BBT required per block to participate on the sidechain
      "MaxStakeToMonthlyIncomeRatio": 0, // The maximum value of `stake` divided by `monthly income` 
    },
    "HistoryNode": {
      "IsActive": false, // Determines if the automatic production as a validator is active
      "MinBBTPerBlock": 0, // The minimum amount of BBT required per block to participate on the sidechain
      "MaxStakeToMonthlyIncomeRatio": 0, // The maximum value of `stake` divided by `monthly income` 
      "MaxSidechainGrowthPerMonthInMB":0 // The maximum amount in MB a sidechain may grow per month
    },
    
    "FullNode": {
      "IsActive": false, // Determines if the automatic production as a validator is active
      "MinBBTPerBlock": 0, // The minimum amount of BBT required per block to participate on the sidechain
      "MaxStakeToMonthlyIncomeRatio": 0, // The maximum value of `stake` divided by `monthly income` 
      "MaxSidechainGrowthPerMonthInMB":0 // The maximum amount in MB a sidechain may grow per month
    },
    "MaxNumberOfSidechains":0, // The maximum number of sidechains the node will work on simultaneously
    "MaxGrowthPerMonthInMB":0 // The maximum growth in MB per month that all sidechains may contribute to
  }
}
```

# Manually Staking and Unstaking BBT
A service requester or service provider may manually add or remove stake to and from the sidechain. There are some restrictions though. A SR can not remove its stake from the sidechain before terminating the sidechain first. Similarly, a SP can not remove its stake before it leaves the production of the sidechain. Also, it doesn't make sense for a SP to add more stake to a sidechain it's already actively producing. It wouldn't help in anything.

It does make sense though, for the SR to regularly add stake to the sidechain to make sure he has enough BBT to pay to the SPs. In order to add more stake to the sidechain, the EOS account associated to the SR sidechain has to have the required amount of BBT to stake.

After following all the above steps you won't have difficulties in finding the corresponding APIs on the "Service Requester" and "Service Provider" APIs. To add stake use the `addStake` service, and to remove stake use the `claimStake` method.


# Querying BlockBase sidechains

To query a BlockBase sidechain, you need to send the query to the following node endpoint of the service requester:

_`apiendpoint`_`/api/Query/ExecuteQuery/`

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
