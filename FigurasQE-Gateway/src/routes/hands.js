const express = require('express');
const router = express.Router();

const multer = require('multer');
const upload = multer();

const grpc = require('@grpc/grpc-js');
const protoLoader = require('@grpc/proto-loader');

const packageDef = protoLoader.loadSync(
  __dirname + '/../proto/fingers.proto',
  {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
  }
);

const proto = grpc.loadPackageDefinition(packageDef).fingers;

const FINGER_GRPC_TARGET = process.env.FINGER_GRPC_TARGET || 'localhost:50051';

const client = new proto.FingerDetector(
  FINGER_GRPC_TARGET,
  grpc.credentials.createInsecure()
);

router.post('/', upload.single('image'), (req, res) => {
  const serviceUnavailableMessage = 'En este momento no es posible jugar los niveles. Por favor, intenta mas tarde.';

  if (!req.file) {
    return res.status(400).json({ error: 'No image provided' });
  }

  const imageBuffer = req.file.buffer;

  const call = client.StreamFrames();

  let responded = false;

  // timeout de seguridad (evita streams colgados)
  const timeout = setTimeout(() => {
    if (!responded) {
      responded = true;
      call.cancel();
      return res.status(503).json({ error: serviceUnavailableMessage });
    }
  }, 8000);

  call.write({
    image_data: imageBuffer,
    client_id: "node-gateway"
  });

  call.end();

  call.on('data', (response) => {

    if (responded) return;

    responded = true;

    clearTimeout(timeout);

    res.json({
      left: response.left_hand,
      right: response.right_hand,
      total: response.total,
      hands: response.hands_detected
    });

    call.cancel(); // importante cerrar stream
  });

  call.on('error', (err) => {

    if (responded) return;

    responded = true;

    clearTimeout(timeout);

    console.error("gRPC error:", err);

    res.status(503).json({ error: serviceUnavailableMessage });

  });

  call.on('end', () => {
    if (responded) return;

    responded = true;
    clearTimeout(timeout);

    res.status(503).json({ error: serviceUnavailableMessage });
  });

});

module.exports = router;
