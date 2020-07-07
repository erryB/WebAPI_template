
# Postman

[[_TOC_]]

## Introduction

This document provides instructions to configure and use Postman as a client for the Web API.

Note that authenticated users can have different roles to perform different actions with the API. They can be either a standard User, a Coordinator or an Admin.

## Prerequisites for B2B/B2C
- The Web API must have the proper configuration in appsettings.Development.json, as described in the [Source README.md](../Source/README.md).
- `WebApi.postman_collection.json` must be imported in Postman.
- Authentication must be configured to call most of the endpoints in the API. To access the Authentication form referenced belowto to Edit collection > Authentication > Type set to OAuth 2.0 > Get New Access Token.

## How to get authentication token

### B2B Authentication

Here are the values that you can copy/paste to fill the form:
| Name | Value |
|------|-------|
|Token Name|`BackendTokenB2B`|
|Grant Type|`Implicit`|
|Callback URL|`https://www.getpostman.com/oauth2/callback`|
|Auth URL|`https://login.microsoftonline.com/<your-aad-account>.onmicrosoft.com/oauth2/v2.0/authorize`|
|Client ID|`<your-app-id-registered-in-aad>`|
|Scope|`<your-scope>`|
|State|`State`|
|Client Authentication|`Send client credentials in body`|

`<your-scope>` can be found in the Azure portal: Azure Active Directory > App Registration > Expose an API > Scopes defined by this API.
All the other settings can be found in appsettings.Development.json in the AzureADB2B section, as described in the [Source README.md](../Source/README.md).

It's necessary to add `https://www.getpostman.com/oauth2/callback` in the Azure portal: Azure Active Directory > App Registration > Authentication > Web platform > Redirect URI. Remember that you need to enable the implicit grant for `Access Token` and `ID tokens` in the Web platform.

### B2C Authentication

Here are the values that you can copy/paste to fill the form:
| Name | Value |
|------|-------|
|Token Name|`BackendTokenB2C`|
|Grant Type|`Implicit`|
|Callback URL|`https://www.getpostman.com/oauth2/callback`|
|Auth URL|`https://<your-b2c-account>.b2clogin.com/<your-b2c-account>.onmicrosoft.com/<your-policy-id-for-signup-signin>/oauth2/v2.0/authorize`|
|Client ID|`<your-app-id-registered-in-b2c>`|
|Scope|`<your-scope>`|
|State|`State`|
|Client Authentication|`Send client credentials in body`|

`<your-scope>` can be found in the Azure portal: go to your B2C directory > App Registration > Expose an API > Scopes defined by this API.
All the other settings can be found in appsettings.Development.json in the AzureADB2C section, as described in the [Source README.md](../Source/README.md).

It's necessary to add `https://www.getpostman.com/oauth2/callback` in the Azure portal: your B2C directory > App Registration > Authentication > Web platform > Redirect URI. Remember that you need to enable the implicit grant for `Access Token` and `ID tokens` in the Web platform.

## APIs in the Postman Collection

### Try if the API is up and running

- **Request**: `Echo`.
- **Requires Authentication**: No.
- **Required Role for Authenticated User**: N/A.
- **Input**: No input required.
- **Output**: A Hello World message.

### Create a User with a B2C user or a B2B user who is native to your Azure Active Directory

- **Request**: `CreateUser`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: No role is required yet. We use this request to ask for a specific role for the user.
- **Input**: Add a Json like the following to the Body of the request:

```
{
  "first_name": "FirstName",
  "last_name": "LastName",
  "role": "User"
}
```

Valid roles are "User", "Coordinator" and Admin. If we don't specify a role, "User" will be used. Note that the email of the user will be extracted from her auth token.

- **Output**: The exact user information that has been inserted in the database.
- **Potential errors**: API will fail if user is not authenticated (as we won't be able to get her email), if we don't provide a first or a last name, if the user already exists in the database, or if the role provided is invalid.

### Create a User with a B2B user who is a guest to your Azure Active Directory.

- **Request**: `CreateUser`.
- **Requires Authentication**: No. B2B guest users will need to use this request to create their user in the database of the API. Then an Admin can update the user and approve her and her requested role (`UpdateUser` request). Once the user is approved she will be invited to B2B Azure Active Directory via Microsoft Graph. Once the user accepts the invite and is part of the directory, she will be able to authenticate and user her token to call other secured endpoints in the API. 
- **Required Role for Authenticated User**: N/A.
- **Input**: Add a Json like the following to the Body of the request:
```
{
  "email": "test@email.com"
  "first_name": "FirstName",
  "last_name": "LastName",
  "role": "User",
  "recaptcha_payload": "payload"
}
```

As user is not authenticated, we cannot get her email from the token, so we need to provide it in the request. Recaptcha payload is required to ensure the endpoint is not being called by a bot. See [Source README.md, section Getting a ReCaptcha token that the API can validate](../Source/README.md) for details on how to get a valid payload.
- **Output**: The exact user information that has been inserted in the database.
- **Potential errors**: API will fail if we don't provide an email, a first or a last name, if the ReCaptcha payload is missing or invalid, if the user already exists in the database, or if the role provided is invalid.

### Get a User

- **Request**: `GetUser`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User, Coordinator or Admin.
- **Input**: Change the email in the request URL to the email of the user you want to get.
- **Output**: The user information found in the database for that email.
- **Potential errors**: Any user can get her own information, but only an Admin can get information from a user other than herself. So API will fail if the user calling the API is not allowed to access the information of the target user, or if the target user doesn't exist in the database.

### Get All Users

- **Request**: `GetAllUsers`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: Admin.
- **Input**: N/A.
- **Output**: The information of all the users found in the database.
- **Potential errors**: API will fail if the user calling the API is not an Admin.

### Update a User

- **Request**: `UpdateUser`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User, Coordinator or Admin.
- **Input**: Change the email in the request URL to the email of the user you want to update, then add a Json like the following to the Body of the request:
```
{
  "email": "email@email.com",
  "first_name": "FirstName",
  "last_name": "LastName",
  "role": "User",
  "status": "Pending"
}
```
All the properties provided in the Body are optional. Any user can update their own first or last name, but only an Admin can update any of the properties for any other target user. Valid user statuses are "Pending", "Rejected" or "Approved".
- **Output**: The latest user information found in the database for that email.
- **Potential errors**: API will fail if the user calling the API is not an Admin and tries to update her email, her role or status, or if she tries to update a target user other than herself. It will also fail if the email, role or status provided are invalid. When using B2B authentication, if an Admin changes the status of target user to "Approved", an invite to join B2B Azure Active Directory will be send to the target email. If the API cannot invite the user, it will also fail.

### Delete a User

- **Request**: `DeleteUser`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: Admin.
- **Input**: Change the email in the request URL to the email of the user you want to delete.
Note: This API will delete a user and all her requests and request details.
- **Output**: N/A.
- **Potential errors**: API will fail if the user calling the API is not an Admin or if the target user cannot be found in the database.

### Create a Request

- **Request**: `CreateRequest`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User.
- **Input**: Add a Json like the following to the Body of the request:
```
{
  "selected_products": [
    {
      "id": "e6f6ddb0-02dd-4106-8716-e6ffa329c664",
      "quantity": 36
    },
    {
      "id": "ce901d35-85d4-45a2-8e14-49bc360f70eb",
      "quantity": 12
    },
    {
      "id": "ad45055b-f1b3-46aa-a4c2-8ba5a4d27236",
      "quantity": 2
    }
  ]
}
```

Those product ids correspond to the only 3 dummy products seeded in the database.

The request will be associated to the user calling the API. The default status for all requests is "Pending".

To update an existing request, you just create a new request with the same reference number:
```
{
  "ref_no": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "selected_products": [
    {
      "id": "e6f6ddb0-02dd-4106-8716-e6ffa329c664",
      "quantity": 3
    }
  ]
}
```
The previous version of the request will be soft-deleted (marked as deleted but still available in the database).

- **Output**: The reference number (`ref_no`) of the request.
- **Potential errors**: API will fail if the user calling the API is not a User, if we didn't provide any selected products, if any product id is invalid, if any quantity is negative, if the reference number we provide is invalid or if the reference number corresponds to a request that belongs to a user that is not the one calling the API.

### Get All Requests

- **Request**: `GetRequests`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User or Coordinator.
- **Input**: N/A.
- **Output**: The lastest version of all requests. A User will only get her own requests, but a Coordinator will get all requests created by all users.
- **Potential errors**: API will fail if the user calling the API doesn't have a valid role.

### Get All Requests as CSV

- **Request**: `GetRequestsCSV`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User or Coordinator.
- **Input**: N/A.
- **Output**: The lastest version of all requests in CSV format. A User will only get her own requests, but a Coordinator will get all requests created by all users.
- **Potential errors**: API will fail if the user calling the API doesn't have a valid role.

### Update a Request

- **Request**: `UpdateRequest`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: Coordinator.
- **Input**: Change the reference number in the request URL to the reference number of the request you want to update, then add a Json like the following to the Body of the request:
```
{
  "status": "Approved"
}
```
Valid statuses for requests are "Pending", "Rejected" or "Approved".
- **Output**: The reference number of the request and its new status.
- **Potential errors**: API will fail if the user calling the API is not a Coordinator, if the reference number is invalid or if the status is missing or invalid.

### Delete a Request

- **Request**: `DeleteRequest`.
- **Requires Authentication**: Yes.
- **Required Role for Authenticated User**: User or Coordinator.
- **Input**: Change the reference number in the request URL to the reference number of the request you want to delete.
This API will delete all versions of the request and their request details.
- **Output**: N/A.
- **Potential errors**: A User can delete her own requests, but only a Coordinator can delete any request. API will fail if the user calling the API is not allowed to delete the request or if the reference number is invalid.
