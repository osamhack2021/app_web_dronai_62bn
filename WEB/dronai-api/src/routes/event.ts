import express from 'express';
import multer from 'multer';

import Event from '../models/event';
import { connection } from '../server/database';


// ==================================================
//                      변수
// ==================================================

// Path
const path = require('path');

// Express
const router = express.Router();

// Multer - storage
const storage = multer.diskStorage({
    // @ts-ignore
    destination: (req, file, callback) => {
        // console.log(req + "," + file);
        callback(null, './resources/images');
    },
    // @ts-ignore
    filename: (req, file, callback) => {
        // console.log(req + "," + file);
        callback(null, file.fieldname + '-' + Date.now() + path.extname(file.originalname));
    }
})

// Multer - upload
const upload = multer({ storage: storage })



// ==================================================
//                      API
// ==================================================
router.post('/upload', upload.array('file', 100), (req, res) => {
    const files = req.files as Express.Multer.File[];
    if (files == null) {
        res.send("[업로드 실패] Array is null");
        res.end();
    }
    else {
        files.forEach(f => {
            res.send(f.destination + "/" + f.filename);
        });
    }

    res.end();
});

router.post('/add', (req, res) => {
    let droneId = req.body.DroneId;
    let detail = req.body.Detail;
    let imgPath = req.body.ImgPath;

    const eventRepository = connection!.getRepository(Event);

    const query = {
        droneId,
        detail,
        imgPath
    };

    eventRepository.save(query).then((u) => {
        res.send(u.droneId + '의 이벤트가 Database에 정상 등록되었습니다.');
    });
});

router.get('/get', (_req, res) => {
    const eventRepository = connection!.getRepository(Event);

    eventRepository.find({}).then((events) => {
        res.json({ success: true, events });
    }).catch(() => res.json({ success: false, msg: "요청 실패" }));

});

router.get('/test', (_req, res) => {
    res.status(200).json({ success: true, msg: '이벤트 API 정상' });
});


export default router;