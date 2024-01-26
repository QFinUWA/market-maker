# First version: log in, view all exchanges, update state of exchange, log out

from dotenv import load_dotenv
import os
import requests
import json

# load the .env file
load_dotenv()

def login():
    # returns the token if successful, None otherwise
    url = "https://market-maker.azurewebsites.net/login"
    headers = {
        "Content-Type": "application/json"
    }
    data = {
        "email": os.getenv("EMAIL"),
        "password": os.getenv("PASSWORD")
    }

    response = requests.post(url, headers=headers, data=json.dumps(data))

    # Check if the request was successful (status code 200)
    if response.status_code == 200:
        print("Login successful!")
        return response.content.decode("utf-8")
    else:
        print(f"Failed to login. Status code: {response.status_code}")
        print(f"Response content: {response.content.decode('utf-8')}")
        return None

# TESTS
assert(login() != None)