"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/hub").build();

connection.on("FileDone", function () {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    li.textContent = "file done";
});

connection.on("SendFile", function (msg) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    li.textContent = "received file chunk";
    setTimeout(function () {
        connection.send("Received");
    }, 1000);
});

connection.on("SendFile2", function (msg) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    li.textContent = "received file chunk";
    const promise = new Promise(function(resolve, reject) {
        setTimeout(function () {
            resolve(true);
        }, 1000);
    });
    return promise;
});

connection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("send1").addEventListener("click", function (event) {
    fetch(`/send?id=${connection.connectionId}`);
    event.preventDefault();
});