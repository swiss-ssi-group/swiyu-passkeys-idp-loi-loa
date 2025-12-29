
const verificationId = document.getElementById('verificationId');

const PENDING = document.getElementById('PENDING');
const SUCCESS = document.getElementById('SUCCESS');
const FAILED = document.getElementById('FAILED');
const PROBLEM = document.getElementById('PROBLEM');
const message = document.getElementById('message'); 

const qrCodeImage = document.getElementById('qrCodeImage'); 
const verifiedData = document.getElementById('verifiedData'); 
const verifiedDataButton = document.getElementById('verifiedDataButton');
const verifiedDataCard = document.getElementById('verifiedDataCard');

if (verificationId != null) {
   
    let checkStatus = setInterval(function () {
        if (verificationId) {

            fetch('../api/register/verification-response?id=' + verificationId.value)
                .then(response => response.text())
                .catch(error => document.getElementById("message").innerHTML = error)
                .then(response => {
                    if (response.length > 0) {
                        let respMsg = JSON.parse(response);
                        console.log("status: " + respMsg["state"])
                        // PENDING, SUCCESS, FAILED
                        if (respMsg.state == 'PENDING') {
                            message.innerHTML = respMsg["state"];
                        }
                        else if (respMsg.state == 'SUCCESS') {
                            message.innerHTML = respMsg["state"];
                            clearInterval(checkStatus)
                            console.log("VC data: " + JSON.stringify(respMsg["wallet_response"]["credential_subject_data"]))

                            PENDING.style.display = "none";
                            SUCCESS.style.display = "initial";
                            FAILED.style.display = "none";
                            PROBLEM.style.display = "none";

                            qrCodeImage.style.display = "none";
                            verifiedDataButton.style.display = "none";
                            verifiedDataCard.style.display = "flex";
                            verifiedData.innerHTML = JSON.stringify(respMsg["wallet_response"]["credential_subject_data"]);
                        }
                        else if (respMsg.state == 'FAILED') {
                            message.innerHTML = respMsg["state"];
                            clearInterval(checkStatus);

                            PENDING.style.display = "none";
                            SUCCESS.style.display = "none";
                            FAILED.style.display = "initial";
                            PROBLEM.style.display = "none";
                        }
                        else {
                            message.innerHTML = "Unknown status: " + respMsg["state"];
                            clearInterval(checkStatus)

                            PENDING.style.display = "none";
                            SUCCESS.style.display = "none";
                            FAILED.style.display = "none";
                            PROBLEM.style.display = "initial";
                        }
                    }
                })
        }

    }, 1500); //change this to higher interval if you use ngrok to prevent overloading the free tier service
}
