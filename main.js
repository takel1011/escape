// Scene setup
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
const cameraEuler = new THREE.Euler(0, 0, 0, 'YXZ'); // YXZ order for FPS controls
const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.shadowMap.enabled = true;
document.body.appendChild(renderer.domElement);

// Physics world
const world = new CANNON.World();
world.gravity.set(0, -9.82, 0); // Set gravity

// Materials for physics
const concreteMaterial = new CANNON.Material('concrete');
const playerMaterial = new CANNON.Material('player');

const concretePlayerContactMaterial = new CANNON.ContactMaterial(
    concreteMaterial,
    playerMaterial,
    {
        friction: 0.1,
        restitution: 0.3
    }
);
world.addContactMaterial(concretePlayerContactMaterial);


// Player
const playerShape = new CANNON.Sphere(1);
const playerBody = new CANNON.Body({ mass: 5, material: playerMaterial });
playerBody.addShape(playerShape);
playerBody.position.set(0, 5, 0);
world.addBody(playerBody);

// Floor
const floorShape = new CANNON.Plane();
const floorBody = new CANNON.Body({ mass: 0, material: concreteMaterial });
floorBody.addShape(floorShape);
floorBody.quaternion.setFromAxisAngle(new CANNON.Vec3(-1, 0, 0), Math.PI * 0.5);
world.addBody(floorBody);

const floorGeometry = new THREE.PlaneGeometry(20, 20);
const floorMaterial = new THREE.MeshStandardMaterial({ color: 0x888888, side: THREE.DoubleSide });
const floor = new THREE.Mesh(floorGeometry, floorMaterial);
floor.rotation.x = -Math.PI / 2;
floor.receiveShadow = true;
scene.add(floor);

// Walls
const wallShape = new CANNON.Box(new CANNON.Vec3(10, 5, 0.1));
const wallBody1 = new CANNON.Body({ mass: 0, material: concreteMaterial });
wallBody1.addShape(wallShape);
wallBody1.position.set(0, 0, -10);
world.addBody(wallBody1);

const wallBody2 = new CANNON.Body({ mass: 0, material: concreteMaterial });
wallBody2.addShape(wallShape);
wallBody2.position.set(0, 0, 10);
world.addBody(wallBody2);

const wallShape2 = new CANNON.Box(new CANNON.Vec3(0.1, 5, 10));
const wallBody3 = new CANNON.Body({ mass: 0, material: concreteMaterial });
wallBody3.addShape(wallShape2);
wallBody3.position.set(-10, 0, 0);
world.addBody(wallBody3);

const wallBody4 = new CANNON.Body({ mass: 0, material: concreteMaterial });
wallBody4.addShape(wallShape2);
wallBody4.position.set(10, 0, 0);
world.addBody(wallBody4);

const roomGeometry = new THREE.BoxGeometry(20, 10, 20);
const roomMaterial = new THREE.MeshStandardMaterial({ color: 0xcccccc, side: THREE.BackSide });
const room = new THREE.Mesh(roomGeometry, roomMaterial);
room.receiveShadow = true;
scene.add(room);





// Buttons for puzzle
const buttonGeometry = new THREE.BoxGeometry(0.5, 0.5, 0.1);
const buttonMaterial = new THREE.MeshStandardMaterial({ color: 0x0000ff }); // Blue
const pressedButtonMaterial = new THREE.MeshStandardMaterial({ color: 0xff0000 }); // Red when pressed

const buttons = [];
const buttonBodies = [];
const buttonPositions = [
    new THREE.Vector3(9.9, 1, -1),
    new THREE.Vector3(9.9, 1, 0),
    new THREE.Vector3(9.9, 1, 1)
];

for (let i = 0; i < buttonPositions.length; i++) {
    const button = new THREE.Mesh(buttonGeometry, buttonMaterial);
    button.position.copy(buttonPositions[i]);
    button.rotation.y = -Math.PI / 2; // Rotate to face the room
    button.name = `button_${i}`; // Identify buttons
    scene.add(button);
    buttons.push(button);

    const buttonShape = new CANNON.Box(new CANNON.Vec3(0.05, 0.25, 0.25)); // Adjust shape for new orientation
    const buttonBody = new CANNON.Body({ mass: 0, material: concreteMaterial });
    buttonBody.addShape(buttonShape);
    buttonBody.position.copy(button.position);
    buttonBody.quaternion.setFromAxisAngle(new CANNON.Vec3(0, 1, 0), -Math.PI / 2); // Adjust physics body rotation
    world.addBody(buttonBody);
    buttonBodies.push(buttonBody);
}

// Puzzle Logic
const correctSequence = [0, 2, 1]; // Example: button 0, then button 2, then button 1
let playerSequence = [];

function checkPuzzle() {
    if (playerSequence.length === correctSequence.length) {
        if (playerSequence.every((val, index) => val === correctSequence[index])) {
            keyFound = true;
            keyIcon.style.display = 'block';
            showMessage('パズルを解いて鍵をゲットしました！');
        } else {
            showMessage('間違った順序です。リセットします。');
            resetPuzzle();
        }
    }
}

function resetPuzzle() {
    playerSequence = [];
    buttons.forEach(button => {
        button.material = buttonMaterial; // Reset color
    });
}

// Combination Lock Box
const boxGeometry = new THREE.BoxGeometry(1, 1, 1);
const boxMaterial = new THREE.MeshStandardMaterial({ color: 0x8B4513 }); // Brown
const lockedBox = new THREE.Mesh(boxGeometry, boxMaterial);
lockedBox.position.set(5, 0.5, 0); // Example position
lockedBox.name = 'lockedBox';
scene.add(lockedBox);

const boxShape = new CANNON.Box(new CANNON.Vec3(0.5, 0.5, 0.5));
const boxBody = new CANNON.Body({ mass: 0, material: concreteMaterial });
boxBody.addShape(boxShape);
boxBody.position.copy(lockedBox.position);
world.addBody(boxBody);

// Combination Hint (on ceiling)
const comboHintCanvas = document.createElement('canvas');
const comboHintContext = comboHintCanvas.getContext('2d');
comboHintCanvas.width = 512;
comboHintCanvas.height = 256;
comboHintContext.font = '30px Arial';
comboHintContext.fillStyle = 'black';
comboHintContext.textAlign = 'center';
comboHintContext.fillText('2, 4, 6, ?', comboHintCanvas.width / 2, comboHintCanvas.height / 2);

const comboHintTexture = new THREE.CanvasTexture(comboHintCanvas);
comboHintTexture.needsUpdate = true; // Force update
const comboHintMaterial = new THREE.MeshBasicMaterial({ map: comboHintTexture, transparent: true, opacity: 1.0, side: THREE.DoubleSide });
const comboHintGeometry = new THREE.PlaneGeometry(2, 1);
const combinationHint = new THREE.Mesh(comboHintGeometry, comboHintMaterial);
combinationHint.position.set(0, 0.1, 0); // On the floor, slightly above
combinationHint.rotation.x = -Math.PI / 2; // Face upwards
scene.add(combinationHint);

// Door
const doorGeometry = new THREE.PlaneGeometry(2, 4);
const doorMaterial = new THREE.MeshStandardMaterial({ color: 0x8B4513 });
const door = new THREE.Mesh(doorGeometry, doorMaterial);
door.position.set(0, 2, -9.9);
scene.add(door);

// Hint Text for Puzzle
const hintCanvas = document.createElement('canvas');
const hintContext = hintCanvas.getContext('2d');
hintCanvas.width = 512;
hintCanvas.height = 256;
hintContext.font = '12px Arial';
hintContext.fillStyle = 'black';
hintContext.textAlign = 'center';
hintContext.fillText('ボタンの順序: 0, 2, 1', hintCanvas.width / 2, hintCanvas.height / 2);

const hintTexture = new THREE.CanvasTexture(hintCanvas);
const hintMaterial = new THREE.MeshBasicMaterial({ map: hintTexture, transparent: true, opacity: 0, side: THREE.DoubleSide });
const hintGeometry = new THREE.PlaneGeometry(8, 4);
const hint = new THREE.Mesh(hintGeometry, hintMaterial);
hint.position.set(-9.9, 2, 0); // Position on a wall (adjusted Y-coordinate)
hint.rotation.y = Math.PI / 2; // Rotate to face the room
scene.add(hint);


// Lighting
const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
scene.add(ambientLight);

const pointLight = new THREE.PointLight(0xffffff, 0.8);
pointLight.position.set(0, 8, 0);
pointLight.castShadow = true;
scene.add(pointLight);

// Controls
const controls = {
    moveForward: false,
    moveBackward: false,
    moveLeft: false,
    moveRight: false,
    turnLeft: false,
    turnRight: false,
    lookUp: false,
    lookDown: false
};

const onKeyDown = (event) => {
    switch (event.code) {
        case 'KeyW': controls.moveForward = true; break;
        case 'KeyA': controls.moveLeft = true; break;
        case 'KeyS': controls.moveBackward = true; break;
        case 'KeyD': controls.moveRight = true; break;
        case 'ArrowLeft': controls.turnLeft = true; break;
        case 'ArrowRight': controls.turnRight = true; break;
        case 'ArrowUp': controls.lookUp = true; break;
        case 'ArrowDown': controls.lookDown = true; break;
    }
};

const onKeyUp = (event) => {
    switch (event.code) {
        case 'KeyW': controls.moveForward = false; break;
        case 'KeyA': controls.moveLeft = false; break;
        case 'KeyS': controls.moveBackward = false; break;
        case 'KeyD': controls.moveRight = false; break;
        case 'ArrowLeft': controls.turnLeft = false; break;
        case 'ArrowRight': controls.turnRight = false; break;
        case 'ArrowUp': controls.lookUp = false; break;
        case 'ArrowDown': controls.lookDown = false; break;
    }
};

document.addEventListener('keydown', onKeyDown);
document.addEventListener('keyup', onKeyUp);

// Raycaster for interaction
const raycaster = new THREE.Raycaster();
const mouse = new THREE.Vector2();
let keyFound = false;
let magnifyingGlassObtained = false;
let isMagnifyingGlassActive = false;
let gameCleared = false; // New flag for game state
const messageElement = document.getElementById('message');
const fadeOverlay = document.getElementById('fade-overlay');
const successScreen = document.getElementById('success-screen');
const keyIcon = document.getElementById('key-icon');
const magnifyingGlassIcon = document.getElementById('magnifying-glass-icon');

// Touch controls variables
const joystickArea = document.getElementById('joystick-area');
const joystickHandle = document.getElementById('joystick-handle');
const lookArea = document.getElementById('look-area');

let joystickActive = false;
let joystickCenter = { x: 0, y: 0 };
let joystickRadius = 0;

let lookActive = false;
let lookStartX = 0;
let lookStartY = 0;

let touchIdentifier = null; // To track specific touch for joystick
let lookTouchIdentifier = null; // To track specific touch for look

function showMessage(msg) {
    messageElement.textContent = msg;
    messageElement.style.display = 'block';
    setTimeout(() => { messageElement.style.display = 'none'; }, 2000);
}

// Function to toggle magnifying glass active state
function toggleMagnifyingGlass() {
    if (magnifyingGlassObtained) {
        isMagnifyingGlassActive = !isMagnifyingGlassActive;
        magnifyingGlassIcon.style.display = isMagnifyingGlassActive ? 'block' : 'none';
        showMessage(isMagnifyingGlassActive ? '虫眼鏡を使用中' : '虫眼鏡を収納しました');
    }
}

function onMouseClick(event) {
    mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;

    raycaster.setFromCamera(mouse, camera);
    raycaster.far = 3; // Set interaction distance to 3 units
    const intersects = raycaster.intersectObjects(scene.children);

    for (let i = 0; i < intersects.length; i++) {
        
        // Check for button interaction
        for (let j = 0; j < buttons.length; j++) {
            if (intersects[i].object === buttons[j]) {
                if (playerSequence.length < correctSequence.length) {
                    playerSequence.push(j);
                    buttons[j].material = pressedButtonMaterial; // Change color to red
                    showMessage(`ボタン ${j} を押しました。`);
                    checkPuzzle();
                }
                return; // Prevent further interaction after a button press
            }
        }

        // Check for locked box interaction
        if (intersects[i].object === lockedBox) {
            const input = prompt('数列の続きを英語で入力してください。');
            if (input.toUpperCase() === 'EIGHT') {
                magnifyingGlassObtained = true;
                showMessage('虫眼鏡をゲットしました！');
                magnifyingGlassIcon.style.display = 'block'; // 虫眼鏡アイコンを表示
                lockedBox.visible = false; // Make box disappear
                world.removeBody(boxBody); // Remove physics body
            } else {
                showMessage('間違っています。');
            }
            return; // Prevent further interaction
        }

        if (intersects[i].object === door) {
            if (keyFound) {
                if (!gameCleared) {
                    gameCleared = true;
                    // Disable controls immediately
                    controls.isLocked = false;
                    document.exitPointerLock();
                    showMessage('脱出成功！');
                }
            } else {
                showMessage('ドアはロックされています。');
            }
        }
    }
}

function onJoystickStart(event) {
    event.preventDefault(); // Prevent scrolling
    if (event.touches.length === 1) {
        joystickActive = true;
        const touch = event.touches[0];
        joystickCenter = { x: joystickArea.offsetLeft + joystickArea.offsetWidth / 2, y: joystickArea.offsetTop + joystickArea.offsetHeight / 2 };
        joystickRadius = joystickArea.offsetWidth / 2;
        joystickHandle.style.left = `${touch.clientX - joystickArea.offsetLeft - joystickHandle.offsetWidth / 2}px`;
        joystickHandle.style.top = `${touch.clientY - joystickArea.offsetTop - joystickHandle.offsetHeight / 2}px`;
        touchIdentifier = touch.identifier;
    }
}

function onJoystickMove(event) {
    event.preventDefault();
    if (!joystickActive) return;

    for (let i = 0; i < event.touches.length; i++) {
        if (event.touches[i].identifier === touchIdentifier) {
            const touch = event.touches[i];
            let dx = touch.clientX - joystickCenter.x;
            let dy = touch.clientY - joystickCenter.y;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance > joystickRadius) {
                dx *= joystickRadius / distance;
                dy *= joystickRadius / distance;
            }

            joystickHandle.style.left = `${joystickCenter.x + dx - joystickArea.offsetLeft - joystickHandle.offsetWidth / 2}px`;
            joystickHandle.style.top = `${joystickCenter.y + dy - joystickArea.offsetTop - joystickHandle.offsetHeight / 2}px`;

            // Update controls based on joystick position
            controls.moveForward = dy < -20;
            controls.moveBackward = dy > 20;
            controls.moveLeft = dx < -20;
            controls.moveRight = dx > 20;
            break;
        }
    }
}

function onJoystickEnd(event) {
    event.preventDefault();
    if (!joystickActive) return;

    let touchEnded = true;
    for (let i = 0; i < event.changedTouches.length; i++) {
        if (event.changedTouches[i].identifier === touchIdentifier) {
            joystickActive = false;
            joystickHandle.style.left = '50%';
            joystickHandle.style.top = '50%';
            joystickHandle.style.transform = 'translate(-50%, -50%)';
            controls.moveForward = false;
            controls.moveBackward = false;
            controls.moveLeft = false;
            controls.moveRight = false;
            touchIdentifier = null;
            break;
        }
    }
}

function onLookStart(event) {
    event.preventDefault();
    if (event.touches.length === 1) {
        lookActive = true;
        const touch = event.touches[0];
        lookStartX = touch.clientX;
        lookStartY = touch.clientY;
        lookTouchIdentifier = touch.identifier;
    }
}

function onLookMove(event) {
    event.preventDefault();
    if (!lookActive) return;

    for (let i = 0; i < event.touches.length; i++) {
        if (event.touches[i].identifier === lookTouchIdentifier) {
            const touch = event.touches[i];
            const dx = touch.clientX - lookStartX;
            const dy = touch.clientY - lookStartY;

            const rotationSpeed = 0.005; // Adjust sensitivity
            cameraEuler.y -= dx * rotationSpeed;
            cameraEuler.x -= dy * rotationSpeed;

            cameraEuler.x = Math.max(-Math.PI / 2, Math.min(Math.PI / 2, cameraEuler.x));
            camera.quaternion.setFromEuler(cameraEuler);

            lookStartX = touch.clientX;
            lookStartY = touch.clientY;
            break;
        }
    }
}

function onLookEnd(event) {
    event.preventDefault();
    if (!lookActive) return;

    for (let i = 0; i < event.changedTouches.length; i++) {
        if (event.changedTouches[i].identifier === lookTouchIdentifier) {
            lookActive = false;
            lookTouchIdentifier = null;
            break;
        }
    }
}

window.addEventListener('click', onMouseClick);

// Touch event listeners
joystickArea.addEventListener('touchstart', onJoystickStart, false);
joystickArea.addEventListener('touchmove', onJoystickMove, false);
joystickArea.addEventListener('touchend', onJoystickEnd, false);

lookArea.addEventListener('touchstart', onLookStart, false);
lookArea.addEventListener('touchmove', onLookMove, false);
lookArea.addEventListener('touchend', onLookEnd, false);

// Prevent context menu on long press
document.addEventListener('contextmenu', (event) => event.preventDefault());

const clock = new THREE.Clock();
let oldElapsedTime = 0;

// Game loop
function animate() {
    requestAnimationFrame(animate);

    const elapsedTime = clock.getElapsedTime();
    const deltaTime = elapsedTime - oldElapsedTime;
    oldElapsedTime = elapsedTime;

    // Update physics world
    world.step(1 / 60, deltaTime, 3);

    // Update player position and camera
    camera.position.copy(playerBody.position);
    camera.position.y += 1; // Adjust camera height relative to player body

    // Update button positions
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].position.copy(buttonBodies[i].position);
        buttons[i].quaternion.copy(buttonBodies[i].quaternion);
    }

    

    // Update button positions
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].position.copy(buttonBodies[i].position);
        buttons[i].quaternion.copy(buttonBodies[i].quaternion);
    }

    // Update button positions
    for (let i = 0; i < buttons.length; i++) {
        buttons[i].position.copy(buttonBodies[i].position);
        buttons[i].quaternion.copy(buttonBodies[i].quaternion);
    }

    // Update locked box position
    if (lockedBox.visible) {
        lockedBox.position.copy(boxBody.position);
        lockedBox.quaternion.copy(boxBody.quaternion);
    }

    // Magnifying glass effect on hint text
    if (magnifyingGlassObtained && isMagnifyingGlassActive) {
        if (hintMaterial.opacity < 1) {
            hintMaterial.opacity += 0.05; // Gradually make hint visible
        }
        // Re-render hint canvas with larger font if not already done
        if (hintContext.font !== '40px Arial') { // Check current font size
            hintContext.clearRect(0, 0, hintCanvas.width, hintCanvas.height);
            hintContext.font = '40px Arial';
            hintContext.fillStyle = 'black';
            hintContext.textAlign = 'center';
            hintContext.fillText('ボタンの順序: 0, 2, 1', hintCanvas.width / 2, hintCanvas.height / 2);
            hintTexture.needsUpdate = true; // Important to update texture
        }
    } else if (magnifyingGlassObtained && !isMagnifyingGlassActive) {
        // If magnifying glass is obtained but not active, hide the hint
        if (hintMaterial.opacity > 0) {
            hintMaterial.opacity -= 0.05; // Gradually hide hint
        }
    } else if (magnifyingGlassObtained && !isMagnifyingGlassActive) {
        // If magnifying glass is obtained but not active, hide the hint
        if (hintMaterial.opacity > 0) {
            hintMaterial.opacity -= 0.05; // Gradually hide hint
        }
    }

    // Player movement
    const rotationSpeed = 0.05;
    cameraEuler.setFromQuaternion(camera.quaternion);

    if (controls.turnLeft) cameraEuler.y += rotationSpeed;
    if (controls.turnRight) cameraEuler.y -= rotationSpeed;
    if (controls.lookUp) cameraEuler.x += rotationSpeed;
    if (controls.lookDown) cameraEuler.x -= rotationSpeed;

    cameraEuler.x = Math.max(-Math.PI / 2, Math.min(Math.PI / 2, cameraEuler.x)); // Limit vertical rotation
    camera.quaternion.setFromEuler(cameraEuler);

    if (!gameCleared) { // Only allow movement if not cleared
        const moveSpeed = 4.0;
        const moveDirection = new THREE.Vector3();
        const right = new THREE.Vector3();
        const cameraDirection = new THREE.Vector3();

        camera.getWorldDirection(cameraDirection);
        // We want to move on the XZ plane
        cameraDirection.y = 0;
        cameraDirection.normalize();

        const forwardSpeed = (Number(controls.moveForward) - Number(controls.moveBackward));
        const strafeSpeed = (Number(controls.moveRight) - Number(controls.moveLeft));

        moveDirection.addScaledVector(cameraDirection, forwardSpeed);
        moveDirection.addScaledVector(right, strafeSpeed);
        
        // Normalize to prevent faster diagonal movement and apply speed
        if (moveDirection.length() > 0) {
            moveDirection.normalize();
            moveDirection.multiplyScalar(moveSpeed);
        }

        playerBody.velocity.x = moveDirection.x;
        playerBody.velocity.z = moveDirection.z;
    } else if (!gameCleared) { // Stop player if not locked and not cleared
        playerBody.velocity.x = 0;
        playerBody.velocity.z = 0;
    }

    // Fade out effect on game clear
    if (gameCleared) {
        let currentOpacity = parseFloat(fadeOverlay.style.opacity);
        if (isNaN(currentOpacity)) currentOpacity = 0;

        if (currentOpacity < 1) {
            currentOpacity += deltaTime * 0.5; // Adjust fade speed
            fadeOverlay.style.opacity = Math.min(currentOpacity, 1);
        } else {
            // Fade complete, show success screen
            successScreen.style.display = 'flex';
        }
    }

    renderer.render(scene, camera);
}
animate();

// Handle window resize
window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}, false);