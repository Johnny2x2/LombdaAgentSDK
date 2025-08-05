# LombdaAgentAPI Testing Guide

## ?? API Token Authentication System Testing

This guide walks you through testing the complete API token authentication system.

### Prerequisites
- Application running on `https://localhost:5001` or `http://localhost:5000`
- Swagger UI accessible at `/swagger`

---

## ?? **Testing Workflow**

### **Step 1: Create an Account (No Authentication Required)**

**Endpoint:** `POST /v1/accounts`

**Request Body:**
```json
{
  "username": "testuser",
  "email": "test@example.com",
  "displayName": "Test User"
}
```

**Expected Response:**
```json
{
  "id": "generated-account-id",
  "username": "testuser",
  "email": "test@example.com",
  "displayName": "Test User",
  "createdAt": "2025-01-01T00:00:00Z",
  "isActive": true
}
```

**?? Save the `id` from the response - you'll need it for the next steps!**

---

### **Step 2: Create an API Token (Using Account ID from Step 1)**

Since we can't authenticate yet, we'll use the `/me/tokens` endpoint but we need to modify our middleware logic to allow token creation for new accounts.

For now, let's create a test endpoint that doesn't require authentication:

**Endpoint:** `POST /v1/accounts/{accountId}/tokens` (we'll create this)

**Request Body:**
```json
{
  "name": "My Test Token",
  "expiresAt": null
}
```

**Expected Response:**
```json
{
  "id": "token-id",
  "name": "My Test Token",
  "token": "ACTUAL-API-TOKEN-HERE", 
  "createdAt": "2025-01-01T00:00:00Z",
  "expiresAt": null,
  "lastUsedAt": null,
  "isActive": true
}
```

**?? IMPORTANT: Copy the `token` value - this is your API key!**

---

### **Step 3: Test Authentication with API Token**

Now use the token from Step 2 to authenticate your requests:

#### **Option A: Using X-API-Key Header**
Add header: `X-API-Key: YOUR-TOKEN-HERE`

#### **Option B: Using Authorization Bearer Header**
Add header: `Authorization: Bearer YOUR-TOKEN-HERE`

#### **Option C: Using Query Parameter** (not recommended)
Add query parameter: `?api_key=YOUR-TOKEN-HERE`

---

### **Step 4: Test Authenticated Endpoints**

**Test 1: Get Your Account Info**
- **Endpoint:** `GET /v1/accounts/me`
- **Headers:** `X-API-Key: YOUR-TOKEN-HERE`
- **Expected:** Your account information

**Test 2: Get Account by ID**
- **Endpoint:** `GET /v1/accounts/{your-account-id}`
- **Headers:** `X-API-Key: YOUR-TOKEN-HERE`
- **Expected:** Your account information

**Test 3: List Your API Tokens**
- **Endpoint:** `GET /v1/accounts/me/tokens`
- **Headers:** `X-API-Key: YOUR-TOKEN-HERE`
- **Expected:** List of your tokens (without actual token values)

**Test 4: Try to Access Another Account (Should Fail)**
- **Endpoint:** `GET /v1/accounts/different-account-id`
- **Headers:** `X-API-Key: YOUR-TOKEN-HERE`
- **Expected:** `403 Forbidden - You can only access your own account`

---

## ?? **Swagger UI Testing**

1. **Create Account:** Use the `POST /v1/accounts` endpoint
2. **Create Token:** Use the new endpoint we'll create
3. **Authenticate in Swagger:** 
   - Click the ?? "Authorize" button in Swagger UI
   - Enter your token in either the "ApiKey" or "Bearer" field
   - Click "Authorize"
4. **Test Protected Endpoints:** Now all requests will include your API key

---

## ?? **Common Issues**

### "You can only access your own account"
- **Cause:** No API key provided or invalid API key
- **Solution:** Make sure you're sending the API key in headers

### "API key is required"  
- **Cause:** Accessing protected endpoint without authentication
- **Solution:** Add the API key header or authenticate in Swagger

### "Invalid API key"
- **Cause:** Wrong token or expired token
- **Solution:** Verify the token is correct and hasn't been revoked

---

## ?? **Database Tables**

The system creates two tables:
- **Accounts:** User account information
- **ApiTokens:** API tokens linked to accounts

Check your `lombdaagent.db` file to see the data.