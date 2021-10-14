import { Column, Entity, PrimaryGeneratedColumn } from 'typeorm';

@Entity()
export default class Event {

    @PrimaryGeneratedColumn('uuid')
    id!: string;

    @Column({ type: 'datetime', default: () => 'CURRENT_TIMESTAMP' })
    date?: string;

    @Column({ type: 'text', nullable: false })
    droneId!: string;

    @Column({ type: 'text', nullable: false })
    detail!: string;

    @Column({ type: 'text', nullable: false })
    imgPath!: string;
}