import express from 'express';
import multer from 'multer';
const path = require('path');


const router = express.Router();
const storage = multer.diskStorage({
    destination: (req, file, callback) => {
        console.log(req + "," + file);
        callback(null, '.../media/images');
    },
    filename: (req, file, callback) => {
        console.log(req + "," + file);
        callback(null, Date.now() + path.extname(file.originalname));
    }
})

const upload = multer({ storage: storage })

router.post("/upload", upload.single('image'), (req, res) => {
    console.log(req);
    res.send("Image upload");
});


router.get('/test', (_req, res) => {
    res.status(200).json({ success: true, msg: '이벤트 API 정상' });
  });

export default router;