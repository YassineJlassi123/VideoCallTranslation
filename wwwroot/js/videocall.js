let localStream;
let peerConnection;
let signalRConnection;
const configuration = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

const startCall = async () => {
    try {
        // Get user-selected languages for translation
        const sourceLang = document.getElementById('sourceLang').value;
        const targetLang = document.getElementById('targetLang').value;

        localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        document.getElementById('localVideo').srcObject = localStream;

        peerConnection = new RTCPeerConnection(configuration);
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

        peerConnection.ontrack = (event) => {
            document.getElementById('remoteVideo').srcObject = event.streams[0];
        };

        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                signalRConnection.invoke("SendIceCandidate", event.candidate);
            }
        };

        await setupSignalRConnection();

        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        await signalRConnection.invoke("SendOffer", offer);

        document.getElementById('startCall').disabled = true;
        document.getElementById('endCall').disabled = false;

        // Start speech recognition with translation
        startSpeechRecognition(sourceLang, targetLang);
    } catch (error) {
        console.error("Error starting the call:", error);
    }
};

const setupSignalRConnection = async () => {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/signalhub")
        .build();

    signalRConnection.on("ReceiveIceCandidate", async (candidate) => {
        try {
            await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
        } catch (error) {
            console.error("Error adding ICE candidate:", error);
        }
    });

    signalRConnection.on("ReceiveOffer", async (offer) => {
        try {
            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);
            await signalRConnection.invoke("SendAnswer", answer);
        } catch (error) {
            console.error("Error handling offer:", error);
        }
    });

    signalRConnection.on("ReceiveAnswer", async (answer) => {
        try {
            await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
        } catch (error) {
            console.error("Error setting remote description:", error);
        }
    });

    await signalRConnection.start();
};

const endCall = async () => {
    // Stop speech recognition if it's ongoing
    if (recognition) {
        recognition.stop(); // Stop the speech recognition
    }

    // Stop any ongoing speech synthesis
    if (speechSynthesis.speaking) {
        speechSynthesis.cancel(); // Cancel any ongoing speech synthesis
    }

    if (peerConnection) {
        peerConnection.close();
    }
    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
    }
    document.getElementById('localVideo').srcObject = null;
    document.getElementById('remoteVideo').srcObject = null;
    document.getElementById('startCall').disabled = false;
    document.getElementById('endCall').disabled = true;
};


const startSpeechRecognition = (sourceLang, targetLang) => {
    const recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition)();
    recognition.continuous = true;
    recognition.lang = sourceLang; // Set recognition language based on user input
    recognition.interimResults = false; // Disable interim results to avoid capturing noise
    let idleTimeout;

    recognition.onresult = async (event) => {
        clearTimeout(idleTimeout); // Reset the silence timer when a result is detected

        const transcript = event.results[event.results.length - 1][0].transcript.trim(); // Trim whitespace

        // Avoid translation if the transcript is empty
        if (!transcript) {
            console.warn("No speech detected, skipping translation.");
            return; // Exit early if there's no transcript
        }

        const translation = await translateText(transcript, sourceLang, targetLang); // Translate based on user input
        document.getElementById('translatedText').textContent = translation;

        // Get available voices
        const voices = speechSynthesis.getVoices();
        const selectedVoice = voices.find(voice => voice.lang.startsWith(targetLang)); // Find voice matching target language

        // Use text-to-speech with the selected voice
        const utterance = new SpeechSynthesisUtterance(translation);
        if (selectedVoice) {
            utterance.voice = selectedVoice; // Set the specific voice for the target language
        }
        speechSynthesis.speak(utterance);
    };

    recognition.onend = () => {
        idleTimeout = setTimeout(() => {
            recognition.start(); // Restart recognition after a short delay
        }, 1000); // Adjust the delay to control how soon it should start again
    };

    recognition.onerror = (event) => {
        console.error("Speech recognition error:", event.error);
        if (event.error !== 'no-speech') { // Ignore no-speech errors and restart only for other errors
            recognition.start();
        }
    };

    recognition.start();
};

// Define translateText function before using it
const translateText = async (text, sourceLang, targetLang) => {
    try {
        const response = await fetch('/Home/Command', { // Use the Command method for translation
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ Text: text, SourceLang: sourceLang, TargetLang: targetLang }),
        });
        if (!response.ok) {
            throw new Error('Translation failed');
        }
        const data = await response.json();
        return data.translatedText;
    } catch (error) {
        console.error("Translation error:", error);
        return "Translation failed";
    }
};

// Define getVoices function before using it
const getVoices = () => {
    const voices = speechSynthesis.getVoices();
    voices.forEach((voice) => {
        console.log(voice.name, voice.lang); // List available voices in the console for debugging
    });
};

// Set the onvoiceschanged event listener
speechSynthesis.onvoiceschanged = getVoices;

// Start fetching voices immediately for the first load
getVoices();

document.getElementById('startCall').addEventListener('click', startCall);
document.getElementById('endCall').addEventListener('click', endCall);
