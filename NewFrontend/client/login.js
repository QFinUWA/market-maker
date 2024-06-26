function bindConnection(jwt) {
    // Called when creating a exchange or joining an exchange
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(
        serverURL + "exchange", {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => jwt,
        })
        // .configureLogging(signalR.LogLevel.Debug)
        .build();

    async function start() {
        try {
        await connection.start();
        console.log("SignalR Connected.");
        } catch (err) {
        console.log(err);
        // setTimeout(start, 50000000);
        }
    }

    connection.onclose(async () => {
        await start();
    });

    return [connection, start];
}

document.getElementById("exchangeForm").onsubmit = async (
    event,
) => {
    // Set error message to hidden
    document
        .getElementById("error-message")
        .classList.add("opacity-0");

    event.preventDefault(); // Prevent the form from submitting in the traditional way
    let exchangeCode =
        document.getElementById("exchange-code").value;
    if (exchangeCode == "") {
        document.getElementById("error-text").innerText =
            "Exchange code cannot be empty";
        document
            .getElementById("error-message")
            .classList.remove("opacity-0");
        return; // Exit if the input is empty
    }

    
    const requestURL =
        serverURL + "joinExchange?exchangeCode=" + exchangeCode;

    try {
        const response = await fetch(requestURL);
        if (!response.ok)
            throw new Error("Network response was not ok."); // Check if the fetch was successful

        const jwt = await response.text(); // Get JWT as text

        let [connection, start] = bindConnection(jwt);
        start().then(() => {
            let name = document.getElementById("name").value;
            let response = connection.invoke("JoinExchange", name);

            response.then(() => {
                // Save JWT as a cookie. Set cookie to expire in 1 day
                const daysValid = 1;
                const expiryDate = new Date();
                expiryDate.setTime(
                    expiryDate.getTime() + daysValid * 24 * 60 * 60 * 1000,
                );
                const expires = "expires=" + expiryDate.toUTCString();
                document.cookie = "jwt=" + jwt + ";" + expires + ";path=/";
                document.cookie = "name=" + document.getElementById("name").value.toLowerCase() + ";" + expires + ";path=/";

                window.location.href = "./index.html";
            }).catch(error => {
                document.getElementById("error-text").innerText =
                    "Name is already taken";
                document
                    .getElementById("error-message")
                    .classList.remove("opacity-0");
            });
        });
        
    } catch (error) {
        console.error("Error joining exchange:", error);
        document.getElementById("error-text").innerText =
            "Error joining exchange: " + error;
        document
            .getElementById("error-message")
            .classList.remove("opacity-0");
        return;
    }


};