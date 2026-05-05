This is the usecase for authentication


"As a new user, I will be able to create an account using my email address and password so that I can access the system."
"As a user, I will be able to log in and out of the system so that I can access my data securely. "
"As a user, I will be able to delete my account and all my data, so that I have control over my personal information."

We are going to use Auth0 for this. For now we are only going to focus on the backend api, not the frontend. This means we are not creating the actual login panel yet, just setting up the backend to work with auth0 
and eventual auth0 users.
Looking at the @domain_model.md in the same folder as this document, we can see that user has the following attributes

    class User {
	    +UUID id PK
	    +String email UK
	    +String phone_number UK
	    +Boolean onboarding_done
	    +Timestamp created_at
	    +Timestamp updated_at
    }
	
We will need to update this to include an auth0 sub so we can link our user datastructures to their auth0 login. Since We
 are focusing on the backend, we may also need to update .env to include a AUTH0.Authority and AUTH0.Audience vars.
 
 
 IMPLEMENTATION DETAILS
 
 1. User provisioning strategy — When a new Auth0 user first hits the API, how does a User row get created in our DB?
	
	ANSWER: When a new auth0 user is created, we should create a User row in our db with the same email. Easiest way to do that is with an Auth0 Action that calls a registration endpoint on our side. Specifically we can use
	 the auth0 "post-user-registration" trigger.

  2. Authorization scope — Auth gives us the sub claim from the JWT. The doc doesn't say how that maps to enforcing data
   ownership (i.e., a user can only see their inventories). Does the sub get resolved to our internal userId in
  middleware, or does each controller/service do it?
  
  ANSWER: Here I believe we should make use of middleware to append our internal userid before the request hits the actual endpoint.
   Keep {userId:guid} in the route, validate via middleware/filter. A filter extracts the Auth0 sub, resolves it to our internal userId, then asserts it matches the route param. If it
   doesn't match → 403. Clean, minimal controller changes. Remember to validate every request.
  
  
  3. Account deletion — Does deleting an account also call the Auth0 Management API to delete the Auth0 user, or do we
  only delete our own DB records?
  
  ANSWER: We delete everything, so both our own db records AND auth0 user.
  
  4. Endpoint protection — All routes protected? Any public exceptions (e.g., health check)?
  
  ANSWER: ONly public exception is /health
  
  5. Config file reference — The doc mentions .env but the project uses appsettings.json. Minor but should be corrected.
  
  ANSWER: We run the app in docker despite what the project says. Dont worry about appsettings.json for now.
  
  6. auth0_sub column spec — Format is provider|{id} (up to ~128 chars). The doc should specify the column type/max
  length.
  
  ANSWER: Here is the column: "auth0_sub VARCHAR(128) NOT NULL UNIQUE"
  
  7. Registration endpoint security — The POST /api/auth/register endpoint is called by Auth0's post-user-registration
  Action. It must be public (AllowAnonymous) since no JWT exists yet, but must not be open to arbitrary callers.
  
  ANSWER: Secure with a shared secret header. The Auth0 Action sends an X-Registration-Secret header containing a
  secret configured in the Action settings. The backend reads this secret from the AUTH0__REGISTRATION_SECRET
  environment variable and returns 401 if the header is missing or does not match. This is a one-liner check in
  the controller.