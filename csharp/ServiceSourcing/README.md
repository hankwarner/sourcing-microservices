﻿
# Inventory Service
API endpoint for requesting inventory data from Manhattan tables. 



## GetInventory
**Type**

`GET`


**Path** 

`http://service-sourcing.supply.com/api/v2/inventory/GetInventory/{masterProductNumber}`


**Headers** 

`Content-Type: application\json`


**Parameters**

_masterProductNumber (string)_: The Ferguson MPN (master product number) for the item, which can be located on a NetSuite item record in the _FEI - Master Product Number_ field.



## Example NetSuite Request
```
var headers = {
	'Content-Type': 'application/json'
};

var response = http.get({
  url: "http://service-sourcing.supply.com/api/v2/inventory/GetInventory/1353735",
  headers: headers
});

var responseBody = JSON.parse(response.body);
```


## Response

Returns a JSON array of `Inventory` objects, each object being a single Ferguson DC or branch:



_branchNumber_ (string): Ferguson branch number, padded with leading zeros for branches (not DC's).

_quantity_ (int): Current quantity of item at the DC/branch.

_stockingStatus_ (string): 'Stocking Item' or 'Non-Stocking Item'. For DC's only. Branches return an 'N/A' status.



### Example Response Body
```
[
	{
	   "branchNumber": "0228",
	   "quantity": 10,
	   "stockingStatus": "N/A"
	},
	{
	   "branchNumber": "321",
	   "quantity": 378,
	   "stockingStatus": "Stocking Item"
	},
	{
	   "branchNumber": "1550",
	   "quantity": 26,
	   "stockingStatus": "N/A"
	},
	{
	   "branchNumber": "2911",
	   "quantity": 0,
	   "stockingStatus": "Non-Stocking Item"
	}
]
```


### Error Response

Returns a `404` response if item is not found. This usually indicates an invalid master product number.

```
if(response.status == 404){
	handleError();
}
```



# Distance Data Service
API endpoint used in determining Precision Delivery Eligibility by calculating distance between a given 
shipping zip and Ferguson branch for a specific item, quantity, and allowed miles from zip.



## GetBranchDistanceData
**Type**

`GET`


**Path** 

`http://service-sourcing.supply.com/api/v2/DistanceData/GetBranchDistanceData/{masterProductNumber}/{quantity}/{zip}/{milesAllowedFromZip}`


**Headers** 

`Content-Type: application\json`


**Parameters**

_masterProductNumber (string)_: The Ferguson MPN (master product number) for the item, which can be located on a NetSuite item 
record in the _FEI - Master Product Number_ field.


_quantity (int)_: Item quantity on the transaction line.


_zip (string)_: Customer's ship-to zip code. Must be US.


_milesAllowedFromZip (int)_: The number of allowed miles between the ship-to zip and the Ferguson branch for a specific Precision Delivery type. 
For example, 30 miles for 2-hour delivery.



## Example NetSuite Request
```
var headers = {
	'Content-Type': 'application/json'
};

var response = http.get({
  url: "http://service-sourcing.supply.com/api/v2/DistanceData/GetBranchDistanceData/1353735/10/29415/30",
  headers: headers
});

var responseBody = JSON.parse(response.body);
```



## Response

Returns a JSON array of `DistanceData` objects, each object being a Ferguson branch that can meet the Precision Delivery request.



_distanceInMiles_ (int): Number of miles the branch is from the ship-to zip.

_branchNumber_ (string): Ferguson branch number, padded with leading zeros so all branch numbers are 4 digits.

_quantity_ (int): Current quantity of item at the branch.

_netSuiteListId_ (string): The ID of the branch in the NetSuite source list.

_error_ (string?): Defaults to `null`, but a more specific 



### Example Response Body
```
[
	{
		"distanceInMiles": 12,
		"branchNumber": 0023,
		"quantity": 24,
		"netSuiteListId": 5,
		"error": null
	}
]
```


**If the response body is an empty array (ie, `responseBody.length == 0`), this means no branches can meet the request.**

```
[]
```



### Error Response
If any of the parameters are invalid, an error will be returned in the `error` property of the response body object:

```
if(responseBody[0].error != null){
	handleError();
}
```


Returns a `404` response if the service is unavailable.

```
if(response.status == 404){
	handleError();
}
```



## PostBranchDistanceData
**Purpose**

This endpoint is similar to `GetBranchDistanceData` and even utilizes the same query; however, it can handle distance data requests 
for multiple items at once.


**Type**

`POST`


**Path** 

`http://service-sourcing.supply.com/api/v2/DistanceData/PostBranchDistanceData`


**Headers** 

`Content-Type: application\json`
`Accept': 'application\json`


**Request Body**

Request body should be a JSON array of the following objects, with each object typically being a different transaction line:


_masterProductNumber (string)_: The Ferguson MPN (master product number) for the item, which can be located on a NetSuite item 
record in the _FEI - Master Product Number_ field.


_quantity (int)_: Item quantity on the transaction line.


_zip (string)_: Customer's ship-to zip code. Must be US.


_milesAllowedFromZip (int)_: The number of allowed miles between the ship-to zip and the Ferguson branch for a specific Precision Delivery type. 
For example, 30 miles for 2-hour delivery.


**Example Request Body***

```
[
  {
	"masterProductNumber": "1353735",
	"quantity": 2,
	"zip": "29415",
	"milesAllowedFromZip": 30
  },
  {
	"masterProductNumber": "1353677",
	"quantity": 5,
	"zip": "29415",
	"milesAllowedFromZip": 30
  }
]
```



## Response

Returns a JSON array of `MultipleItemsDistanceData` objects, with each object having a Master Product Number and a nested array 
of `DistanceData` objects, similar to the `GetBranchDistanceData` response.



### Example Response Body
```
[
  {
	"masterProductNumber": "1353735",
	"distanceData": [
	  {
		"distanceInMiles": 1,
		"branchNumber": 23,
		"quantity": 5,
		"netSuiteListId": 36,
		"error": null
	  }
	],
	"error": null
  },
  {
	"masterProductNumber": "1353677",
	"distanceData": [
	  {
		"distanceInMiles": 1,
		"branchNumber": 23,
		"quantity": 5,
		"netSuiteListId": 36,
		"error": null
	  }
	],
	"error": null
  }
]
```


**If distanceData is an empty array, this means no branches can meet the request for the specific line item.**

```
[
  {
	"masterProductNumber": "1353735",
	"distanceData": [],
	"error": null
  }
]
```



### Error Response
If the request body is invalid, an error will be returned in the `error` property of the response body object:

```
if(responseBody[0].error != null){
	handleError();
}
```


Returns a `404` response if the service is unavailable.

```
if(response.status == 404){
	handleError();
}
```



## GetLocationsWithinMiles
**Description**

Returns the NetSuite _FEI - Ship From Location_ record list ID and distance in miles of Ferguson DC's and branches 
from the given zip code. Optionally, providing the `distanceInMiles` parameter will limit the results set to only DC's/branches
within the specified number of miles.


**Type**

`GET`


**Path** 

`http://service-sourcing.supply.com/api/v2/DistanceData/GetLocationsWithinMiles/{zip}/{distanceInMiles?}`


**Headers** 

`Content-Type: application\json`


**Parameters**

_zip (string)_: The zip code you would like to see DC's/branch distance data for. Starting point of the search.


_distanceInMiles (int?) *Optional_: Optionally limits the results set to only DC's/branches within the specific range (in miles).



## Example NetSuite Request
```
var headers = {
	'Content-Type': 'application/json'
};

var response = http.get({
  url: "http://service-sourcing.supply.com/api/v2/DistanceData/GetLocationsWithinMiles/30316/30",
  headers: headers
});

var responseBody = JSON.parse(response.body);
```



## Response

Returns a JSON array of `DistanceData` objects, each object being a Ferguson DC/branch, with the NetSuite list ID 
and distance from the given zip code.



### Example Response Body
```
[
  {
	"locationNetSuiteId": 3,
	"distanceFromZip": 128.4945931904,
	"branchNumber": 533,
	"error": null
  },
  {
	"locationNetSuiteId": 17,
	"distanceFromZip": 151.3865295696,
	"branchNumber": 3,
	"error": null
  },
  {
	"locationNetSuiteId": 111,
	"distanceFromZip": 29.8233321152,
	"branchNumber": 107,
	"error": null
  },
  {
	"locationNetSuiteId": 147,
	"distanceFromZip": 124.4861275792,
	"branchNumber": 150,
	"error": null
  }
]
```


### Error Response
If any of the parameters are invalid, an error will be returned in the `error` property of the response body object:

```
if(responseBody[0].error != null){
	handleError();
}
```


Returns a `404` response if the service is unavailable.

```
if(response.status == 404){
	handleError();
}
```

# Twilio Service
API endpoint for Sending a Text Message.



## SendTextMessage
**Type**

`POST`


**Path** 

`http://service-sourcing.supply.com/api/v2/Twilio/SendTextMessage`


**Headers** 

`Content-Type: application\json`


**Parameters**

    	var smsObj = {
  			  phone: "555-555-5555",
  			  message: "message"
  		}



## Example NetSuite Request
```
var headers = {
	'Content-Type': 'application/json'
};

var response = http.post({
  url: "http://service-sourcing.supply.com/api/v2/Twilio/SendTextMessage",
  headers: headers,
  body : JSON.stringify(smsObj),
});

var responseBody = JSON.parse(response.body);
```


## Response

Returns a string of Success, or an error string.




### Example Response Body
```
Text Successfully sent to {Phone}!
```