const apiBaseUrl = (window.FIGURAS_API_BASE_URL || "http://localhost:3000").replace(/\/$/, "");
const apiRoute = `${apiBaseUrl}/hands`;
const video = document.getElementById("video");
const canvas = document.getElementById("canvas");
const ctx = canvas.getContext("2d");
const counters = document.getElementById("counter-container");
const config = document.getElementById("level-config");
const musicToggle = document.getElementById("music-toggle");
const levelInstructions = document.getElementById("level-instructions");
const levelInstructionsTitle = document.getElementById("level-instructions-title");
const levelInstructionsBody = document.getElementById("level-instructions-body");
const leftCloudCount = document.getElementById("left-cloud-count");
const rightCloudCount = document.getElementById("right-cloud-count");
const totalCloudCount = document.getElementById("total-cloud-count");
let status = document.getElementById("hands-status");

if (!status) {
    status = document.createElement("div");
    status.id = "hands-status";
    status.className = "hands-status";
    document.body.appendChild(status);
}

const nextLevelRoute = config.dataset.nextLevel;
const expectedTotal = Number(config.dataset.total);
const expectedLeft = Number(config.dataset.left);
const expectedRight = Number(config.dataset.right);
const currentLevelId = Number(window.location.pathname.match(/Level(\d+)/i)?.[1] ?? 0);
const levelStartedAt = Date.now();

const countersArr = [];
Array.from(counters.children).forEach(child => {
    countersArr.push(child);
});

let lastResult = null;
let resultStartTime = null;
let actionTriggered = false;
let lastLeftFingerCount = 0;
let lastRightFingerCount = 0;
let lastErrorSignature = "";
let leftStageUnlocked = false;

const leftContainer = document.getElementById("left-hand-container");
Array.from(leftContainer.children).forEach(child => {
    child.setAttribute('data-bs-theme', 'dark');
});
const rightContainer = document.getElementById("right-hand-container");
Array.from(rightContainer.children).forEach(child => {
    child.setAttribute('data-bs-theme', 'dark');
});

canvas.width = 320;
canvas.height = 240;

let processing = false;
let cameraReady = false;
let lastStatusText = "";
let lastStatusAt = 0;
let musicContext = null;
let musicMaster = null;
let musicTimer = null;
let musicIsOn = false;
let effectsMaster = null;
let audioWasPrimed = false;
let completionNavigationTimer = null;
let instructionsHideTimer = null;
let instructionsRemoveTimer = null;
const musicPreferenceKey = "fqe-level-music";
const calmChords = [
    [261.63, 329.63, 392.00],
    [293.66, 349.23, 440.00],
    [246.94, 329.63, 392.00],
    [220.00, 277.18, 329.63]
];

showLevelInstructions();

function setHandsStatus(message, isError = false, force = false) {
    if (!status) return;

    const now = Date.now();
    if (!force && message === lastStatusText && now - lastStatusAt < 1000) return;

    lastStatusText = message;
    lastStatusAt = now;
    status.textContent = message;
    status.classList.toggle("hands-status-error", isError);
}

function updateCloudCounts(leftCount, rightCount) {
    if (leftCloudCount) {
        leftCloudCount.textContent = String(leftCount);
        leftCloudCount.classList.toggle("finger-sum-over", leftCount > expectedLeft);
    }

    if (rightCloudCount) {
        rightCloudCount.textContent = String(rightCount);
        rightCloudCount.classList.toggle("finger-sum-over", rightCount > expectedRight);
    }

    if (totalCloudCount) {
        totalCloudCount.textContent = String(leftCount + rightCount);
    }
}

function updateMusicButton() {
    if (!musicToggle) return;

    musicToggle.textContent = musicIsOn ? "Musica on" : "Musica";
    musicToggle.classList.toggle("music-on", musicIsOn);
    musicToggle.setAttribute("aria-pressed", musicIsOn.toString());
}

function showLevelInstructions() {
    if (!levelInstructions || !levelInstructionsTitle || !levelInstructionsBody) return;

    levelInstructionsTitle.textContent = `Este nivel quiere que sumes ${expectedTotal}.`;
    levelInstructionsBody.textContent = buildInstructionSentence(expectedLeft, expectedRight);
    showInstructionCard(4200);
}

function buildInstructionSentence(leftCount, rightCount) {
    if (leftCount === 0 && rightCount === 0) {
        return "Por ahora manten tus manos descansando.";
    }

    if (rightCount === 0) {
        return `Necesitas levantar ${fingerWord(leftCount)} en tu mano izquierda.`;
    }

    if (leftCount === 0) {
        return `Necesitas levantar ${fingerWord(rightCount)} en tu mano derecha.`;
    }

    return `Primero levanta ${fingerWord(leftCount)} en tu mano izquierda. Cuando eso este correcto, agrega ${fingerWord(rightCount)} en tu mano derecha.`;
}

function fingerWord(count) {
    return count === 1 ? "1 dedo" : `${count} dedos`;
}

function showRightHandPrompt() {
    if (!levelInstructions || !levelInstructionsTitle || !levelInstructionsBody || expectedRight <= 0) return;

    levelInstructionsTitle.textContent = "Muy bien.";
    levelInstructionsBody.textContent = `Ahora levanta ${fingerWord(expectedRight)} en tu mano derecha.`;
    showInstructionCard(2200);
}

function showInstructionCard(durationMs) {
    if (!levelInstructions) return;

    if (instructionsHideTimer) {
        window.clearTimeout(instructionsHideTimer);
    }

    if (instructionsRemoveTimer) {
        window.clearTimeout(instructionsRemoveTimer);
    }

    levelInstructions.hidden = false;
    levelInstructions.classList.remove("level-instructions-hide");

    instructionsHideTimer = window.setTimeout(() => {
        levelInstructions.classList.add("level-instructions-hide");
        instructionsRemoveTimer = window.setTimeout(() => {
            if (levelInstructions) {
                levelInstructions.hidden = true;
            }
        }, 350);
    }, durationMs);
}

async function toggleMusic() {
    if (musicIsOn) {
        stopMusic();
        localStorage.setItem(musicPreferenceKey, "off");
        return;
    }

    await startMusic();
    localStorage.setItem(musicPreferenceKey, "on");
}

async function startMusic() {
    const context = getAudioContext();
    if (!context) return;

    await resumeAudioContext();

    musicMaster.gain.cancelScheduledValues(musicContext.currentTime);
    musicMaster.gain.setTargetAtTime(0.045, musicContext.currentTime, 0.25);

    if (musicIsOn) return;

    musicIsOn = true;
    updateMusicButton();

    let chordIndex = 0;
    const playNextChord = () => {
        if (!musicIsOn || !musicContext || !musicMaster) return;

        playCalmChord(calmChords[chordIndex % calmChords.length]);
        chordIndex += 1;
        musicTimer = window.setTimeout(playNextChord, 3800);
    };

    playNextChord();
}

function stopMusic() {
    musicIsOn = false;
    updateMusicButton();

    if (musicTimer) {
        window.clearTimeout(musicTimer);
        musicTimer = null;
    }

    if (musicMaster && musicContext) {
        musicMaster.gain.cancelScheduledValues(musicContext.currentTime);
        musicMaster.gain.setTargetAtTime(0.0001, musicContext.currentTime, 0.35);
    }
}

function playCalmChord(frequencies) {
    const startAt = musicContext.currentTime;
    const duration = 4.2;
    const chordGain = musicContext.createGain();
    const filter = musicContext.createBiquadFilter();

    chordGain.gain.setValueAtTime(0.0001, startAt);
    chordGain.gain.exponentialRampToValueAtTime(0.22, startAt + 0.8);
    chordGain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration);

    filter.type = "lowpass";
    filter.frequency.value = 900;
    filter.Q.value = 0.8;
    filter.connect(musicMaster);
    chordGain.connect(filter);

    frequencies.forEach((frequency, index) => {
        const oscillator = musicContext.createOscillator();
        const detune = index === 1 ? 4 : index === 2 ? -5 : 0;

        oscillator.type = "sine";
        oscillator.frequency.value = frequency;
        oscillator.detune.value = detune;
        oscillator.connect(chordGain);
        oscillator.start(startAt);
        oscillator.stop(startAt + duration + 0.1);
    });
}

function getAudioContext() {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return null;

    if (!musicContext) {
        musicContext = new AudioContext();

        musicMaster = musicContext.createGain();
        musicMaster.gain.value = 0.045;
        musicMaster.connect(musicContext.destination);

        effectsMaster = musicContext.createGain();
        effectsMaster.gain.value = 0.22;
        effectsMaster.connect(musicContext.destination);
    }

    return musicContext;
}

async function resumeAudioContext() {
    const context = getAudioContext();
    if (!context) return false;

    if (context.state === "suspended") {
        await context.resume();
    }

    return context.state === "running";
}

async function playCounterSound(step) {
    if (!await resumeAudioContext() || !effectsMaster) return;

    const startAt = musicContext.currentTime + 0.12;
    const oscillator = musicContext.createOscillator();
    const gain = musicContext.createGain();
    const filter = musicContext.createBiquadFilter();
    const frequencies = [660, 784, 988];

    oscillator.type = "triangle";
    oscillator.frequency.setValueAtTime(frequencies[step] ?? 784, startAt);
    oscillator.frequency.exponentialRampToValueAtTime((frequencies[step] ?? 784) * 1.08, startAt + 0.08);

    filter.type = "lowpass";
    filter.frequency.value = 2400;

    gain.gain.setValueAtTime(0.0001, startAt);
    gain.gain.exponentialRampToValueAtTime(1.0, startAt + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, startAt + 0.24);

    oscillator.connect(filter);
    filter.connect(gain);
    gain.connect(effectsMaster);

    oscillator.start(startAt);
    oscillator.stop(startAt + 0.26);
}

async function playFingerGlowSound(step) {
    if (!await resumeAudioContext() || !effectsMaster) return;

    const startAt = musicContext.currentTime + 0.06;
    const oscillator = musicContext.createOscillator();
    const gain = musicContext.createGain();
    const filter = musicContext.createBiquadFilter();
    const frequencies = [523.25, 587.33, 659.25, 698.46, 783.99];

    oscillator.type = "sine";
    oscillator.frequency.setValueAtTime(frequencies[step % frequencies.length], startAt);
    oscillator.frequency.exponentialRampToValueAtTime(frequencies[step % frequencies.length] * 1.025, startAt + 0.26);

    filter.type = "lowpass";
    filter.frequency.value = 1300;

    gain.gain.setValueAtTime(0.0001, startAt);
    gain.gain.exponentialRampToValueAtTime(0.32, startAt + 0.08);
    gain.gain.exponentialRampToValueAtTime(0.0001, startAt + 0.52);

    oscillator.connect(filter);
    filter.connect(gain);
    gain.connect(effectsMaster);

    oscillator.start(startAt);
    oscillator.stop(startAt + 0.56);
}

async function playFingerErrorSound() {
    if (!await resumeAudioContext() || !effectsMaster) return;

    const startAt = musicContext.currentTime + 0.05;
    const oscillator = musicContext.createOscillator();
    const gain = musicContext.createGain();
    const filter = musicContext.createBiquadFilter();

    oscillator.type = "sine";
    oscillator.frequency.setValueAtTime(392, startAt);
    oscillator.frequency.exponentialRampToValueAtTime(293.66, startAt + 0.22);

    filter.type = "lowpass";
    filter.frequency.value = 1000;

    gain.gain.setValueAtTime(0.0001, startAt);
    gain.gain.exponentialRampToValueAtTime(0.5, startAt + 0.04);
    gain.gain.exponentialRampToValueAtTime(0.0001, startAt + 0.34);

    oscillator.connect(filter);
    filter.connect(gain);
    gain.connect(effectsMaster);

    oscillator.start(startAt);
    oscillator.stop(startAt + 0.38);
}

function primeAudio() {
    if (audioWasPrimed) return;

    audioWasPrimed = true;
    resumeAudioContext().catch(err => console.error("Audio prime error:", err));
}

function activateCounter(index) {
    const counter = countersArr[index];
    if (!counter || counter.classList.contains("active-counter")) return;

    counter.classList.add("active-counter");
    window.setTimeout(() => {
        playCounterSound(index).catch(err => console.error("Counter sound error:", err));
    }, 40);
}

musicToggle?.addEventListener("click", () => {
    toggleMusic().catch(err => console.error("Music error:", err));
});

window.addEventListener("pointerdown", primeAudio, { once: true });
window.addEventListener("keydown", primeAudio, { once: true });

updateMusicButton();

if (localStorage.getItem(musicPreferenceKey) === "on") {
    musicToggle?.classList.add("music-on");
    musicToggle && (musicToggle.textContent = "Musica");
}

async function turnOnCamera() {
    setHandsStatus("Solicitando permiso de camara...", false, true);

    try {
        if (!navigator.mediaDevices?.getUserMedia) {
            setHandsStatus("Este navegador no permite abrir la camara aqui.", true, true);
            return;
        }

        const stream = await navigator.mediaDevices.getUserMedia({
            video: {
                width: 640,
                height: 480
            },
            audio: false
        });

        video.srcObject = stream;
        video.onloadedmetadata = async () => {
            await video.play();
            cameraReady = true;
            setHandsStatus("Camara lista. Detectando manos...", false, true);
            startFramesCapture();
        };
    } catch (err) {
        console.error("Camera error:", err);
        setHandsStatus(`No se pudo abrir la camara: ${err.name || err.message}`, true, true);
    }
}

function startFramesCapture() {
    setInterval(async () => {
        if (!cameraReady) return;
        if (processing) return;
        processing = true;
        
        try {
            await captureFrame();
        } catch (err) {
            console.error("Frame capture error:", err);
        } finally {
            processing = false;
        }
    }, 450);
}

async function captureFrame() {
    ctx.drawImage(
        video, 0, 0, canvas.width, canvas.height
    );

    const blob = await new Promise(resolve => {
        canvas.toBlob(
            resolve, "image/jpeg", 0.8
        );
    });

    if (!blob) return;

    const formData = new FormData();

    formData.append(
        "image", blob, "frame.jpg"
    );

    const response = await fetch(apiRoute, {
        method: "POST",
        body: formData
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error("Backend error:", response.status, errorText);
        setHandsStatus(resolveFriendlyLevelError(errorText), true, true);
        return;
    }

    const data = await response.json();

    validateResult(data);

    setHandsStatus(`Manos: ${data.hands ? "si" : "no"} | Izq ${data.left} Der ${data.right} Total ${data.total}`);
    updateCloudCounts(data.left, data.right);
    updateHands(data);
}

function resolveFriendlyLevelError(rawErrorText) {
    if (!rawErrorText) {
        return "En este momento no es posible jugar los niveles. Por favor, intenta mas tarde.";
    }

    try {
        const parsed = JSON.parse(rawErrorText);
        if (parsed?.error && typeof parsed.error === "string") {
            return parsed.error;
        }
    } catch (error) {
        console.error("Friendly error parse failed:", error);
    }

    return "En este momento no es posible jugar los niveles. Por favor, intenta mas tarde.";
}

function validateResult(data) {
    const leftIsCorrect = data.left === expectedLeft;
    const rightIsCorrect = data.right === expectedRight;
    const totalIsCorrect = data.total === expectedTotal;
    const rightStayedDown = data.right === 0;

    if (!leftStageUnlocked) {
        if (leftIsCorrect && (expectedRight === 0 || rightStayedDown)) {
            leftStageUnlocked = true;
            showRightHandPrompt();
        } else {
            playOverTargetErrorIfNeeded(data);
            resetCounterProgress(false);
            return;
        }
    }

    if (!leftIsCorrect) {
        leftStageUnlocked = false;
        playOverTargetErrorIfNeeded(data);
        resetCounterProgress(false);
        return;
    }

    const isExpectedGesture = totalIsCorrect && leftIsCorrect && rightIsCorrect;

    if (!isExpectedGesture) {
        playOverTargetErrorIfNeeded(data);
        resetCounterProgress(false);
        return;
    }

    lastErrorSignature = "";

    if (!resultStartTime) {
        resultStartTime = Date.now();
    }

    const elapsed = Date.now() - resultStartTime;

    if (elapsed >= 1000) {
        activateCounter(0);
    }

    if (elapsed >= 2000) {
        activateCounter(1);
    }

    if (elapsed >= 3000 && !actionTriggered) {
        actionTriggered = true;
        activateCounter(2);

        const completionId = crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
        const finishingTime = Math.max(0, Date.now() - levelStartedAt);
        const params = new URLSearchParams({
            nextLevel: nextLevelRoute,
            levelId: currentLevelId.toString(),
            finishingTime: finishingTime.toString(),
            completionId
        });

        completionNavigationTimer = window.setTimeout(() => {
            window.location.href = `/Levels/LevelComplete?${params.toString()}`;
        }, 420);
    }
}

function playOverTargetErrorIfNeeded(data) {
    const leftIsOver = data.left > expectedLeft;
    const rightIsOver = data.right > expectedRight;

    if (!leftIsOver && !rightIsOver) {
        lastErrorSignature = "";
        return;
    }

    const errorSignature = `${data.left}-${data.right}`;
    if (errorSignature === lastErrorSignature) return;

    lastErrorSignature = errorSignature;
    playFingerErrorSound().catch(err => console.error("Finger error sound error:", err));
}

function resetCounterProgress(resetLeftStage = true) {
    lastResult = null;
    resultStartTime = null;
    actionTriggered = false;
    if (resetLeftStage) {
        leftStageUnlocked = false;
    }

    if (completionNavigationTimer) {
        window.clearTimeout(completionNavigationTimer);
        completionNavigationTimer = null;
    }

    cleanCounters();
}

function cleanCounters() {
    countersArr.forEach(child => {
        child.classList.remove("active-counter");
    });
}

function updateHands(data) {
    let leftFingers = [];
    let rightFingers = [];
    Array.from(leftContainer.children).forEach(child => {
        leftFingers.push(child);
    });
    Array.from(rightContainer.children).forEach(child => {
        rightFingers.push(child);
    });

    const rightLocked = expectedRight > 0 && !leftStageUnlocked;
    rightFingers.forEach(finger => {
        finger.classList.toggle("locked-finger", rightLocked);
    });

    updateFingerGroup(leftFingers, data.left, lastLeftFingerCount, expectedLeft);
    updateFingerGroup(rightFingers, data.right, lastRightFingerCount, expectedRight, rightLocked);

    lastLeftFingerCount = data.left;
    lastRightFingerCount = data.right;
}

function updateFingerGroup(fingers, activeCount, previousCount, expectedCount, isLocked = false) {
    const isOverTarget = activeCount > expectedCount;

    fingers.forEach((finger, index) => {
        const shouldBeActive = index < activeCount;
        const shouldShowBlue = shouldBeActive && !isOverTarget && !isLocked;
        const shouldShowRed = shouldBeActive && isOverTarget;

        finger.classList.toggle("active-finger", shouldShowBlue);
        finger.classList.toggle("error-finger", shouldShowRed);
    });

    if (isOverTarget || isLocked) return;

    for (let i = 0; i < activeCount; i++) {
        if (i >= previousCount) {
            playFingerGlowSound(i).catch(err => console.error("Finger sound error:", err));
        }
    }
}

turnOnCamera();
